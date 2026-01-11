using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LuxDrive.Data;
using LuxDrive.Data.Models;
using System.Text.RegularExpressions;
using System.Globalization;

namespace LuxDrive.Controllers
{
    public class PricingController : Controller
    {
        private readonly LuxDriveDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public PricingController(LuxDriveDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private int GetPlanRank(string plan)
        {
            return plan switch
            {
                "Enterprise" => 3,
                "Pro" => 2,
                "Basic" => 1,
                _ => 0
            };
        }

        private string GetUserKey(string baseKey)
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated) return baseKey;
            string safeUserName = User.Identity.Name.Replace("@", "_").Replace(".", "_");
            return $"{baseKey}_{safeUserName}";
        }

        public async Task<IActionResult> Index()
        {
            string currentPlan = "None";
            string expiryDateStr = "";
            bool hasCard = false;

            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    hasCard = await _context.PaymentCards.AnyAsync(c => c.UserId == user.Id.ToString());

                    string planKey = GetUserKey("CurrentPlan");
                    string expiryKey = GetUserKey("PlanExpiry");

                    currentPlan = Request.Cookies[planKey] ?? "None";
                    string storedDate = Request.Cookies[expiryKey];

                    if (currentPlan != "None" && !string.IsNullOrEmpty(storedDate))
                    {
                        if (DateTime.TryParse(storedDate, out DateTime expiryDate))
                        {
                            if (DateTime.Now > expiryDate)
                            {
                                if (hasCard)
                                {
                                    expiryDate = DateTime.Now.AddMonths(1);
                                    CookieOptions option = new CookieOptions { Expires = DateTime.Now.AddDays(400) };
                                    Response.Cookies.Append(expiryKey, expiryDate.ToString(), option);
                                    expiryDateStr = expiryDate.ToString("dd.MM.yyyy");
                                }
                                else
                                {
                                    currentPlan = "None";
                                    Response.Cookies.Delete(planKey);
                                    Response.Cookies.Delete(expiryKey);
                                    expiryDateStr = "";
                                }
                            }
                            else
                            {
                                expiryDateStr = expiryDate.ToString("dd.MM.yyyy");
                            }
                        }
                    }
                }
            }

            ViewBag.CurrentPlan = currentPlan;
            ViewBag.HasCard = hasCard;
            ViewBag.ExpiryDate = expiryDateStr;

            return View();
        }

        [Authorize]
        public IActionResult Checkout(string plan)
        {
            if (string.IsNullOrEmpty(plan)) return RedirectToAction(nameof(Index));
            ViewBag.Plan = plan;
            return View();
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Process(string cardNumber, string expiry, string cvc, string cardName, string plan)
        {
            if (string.IsNullOrEmpty(cardName) || !Regex.IsMatch(cardName, @"^[a-zA-Zа-яА-Я\s\-]+$"))
            {
                TempData["ErrorMessage"] = "Card name must contain only letters.";
                return RedirectToAction("Checkout", new { plan = plan });
            }

            if (string.IsNullOrEmpty(cardNumber) || cardNumber.Replace(" ", "").Length < 15)
            {
                TempData["ErrorMessage"] = "Invalid card number.";
                return RedirectToAction("Checkout", new { plan = plan });
            }

            if (!string.IsNullOrEmpty(expiry) && expiry.Contains("/"))
            {
                var parts = expiry.Split('/');
                if (int.TryParse(parts[0], out int month))
                {
                    if (month < 1 || month > 12)
                    {
                        TempData["ErrorMessage"] = "Invalid month! Please enter 01 to 12.";
                        return RedirectToAction("Checkout", new { plan = plan });
                    }
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Invalid expiry date format.";
                return RedirectToAction("Checkout", new { plan = plan });
            }

            if (string.IsNullOrEmpty(cvc) || cvc.Length < 3)
            {
                TempData["ErrorMessage"] = "CVC must be at least 3 digits.";
                return RedirectToAction("Checkout", new { plan = plan });
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            await Task.Delay(2000);

            try
            {
                string cleanNumber = cardNumber.Replace(" ", "").Trim();
                string last4 = cleanNumber.Length >= 4 ? cleanNumber.Substring(cleanNumber.Length - 4) : cleanNumber;

                string cardType = "unknown";
                if (cleanNumber.StartsWith("4")) cardType = "visa";
                else if (cleanNumber.StartsWith("5")) cardType = "mastercard";
                else if (cleanNumber.StartsWith("3")) cardType = "amex";

                bool exists = await _context.PaymentCards.AnyAsync(c => c.UserId == user.Id.ToString() && c.CardLast4 == last4);

                if (!exists)
                {
                    var newCard = new PaymentCard
                    {
                        UserId = user.Id.ToString(),
                        CardLast4 = last4,
                        CardType = cardType
                    };
                    _context.PaymentCards.Add(newCard);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception) { }

            string planKey = GetUserKey("CurrentPlan");
            string expiryKey = GetUserKey("PlanExpiry");

            DateTime validUntil = DateTime.Now.AddMonths(1);
            CookieOptions option = new CookieOptions { Expires = DateTime.Now.AddDays(400) };

            Response.Cookies.Append(planKey, plan, option);
            Response.Cookies.Append(expiryKey, validUntil.ToString(), option);

            TempData["SuccessMessage"] = $"Successfully activated {plan} plan! Valid until {validUntil:dd.MM.yyyy}.";
            return RedirectToAction("Index");
        }

        [Authorize]
        public async Task<IActionResult> QuickPurchase(string plan)
        {
            var user = await _userManager.GetUserAsync(User);
            bool hasCardInDb = await _context.PaymentCards.AnyAsync(c => c.UserId == user.Id.ToString());

            if (!hasCardInDb)
            {
                TempData["ErrorMessage"] = "No saved card found. Please add a card.";
                return RedirectToAction("Checkout", new { plan = plan });
            }

            string planKey = GetUserKey("CurrentPlan");
            string expiryKey = GetUserKey("PlanExpiry");

            await Task.Delay(1500);

            DateTime validUntil = DateTime.Now.AddMonths(1);
            CookieOptions option = new CookieOptions { Expires = DateTime.Now.AddDays(400) };

            Response.Cookies.Append(planKey, plan, option);
            Response.Cookies.Append(expiryKey, validUntil.ToString(), option);

            TempData["SuccessMessage"] = $"Successfully upgraded to {plan} plan!";
            return RedirectToAction("Index");
        }

        [Authorize]
        public IActionResult Downgrade()
        {
            string planKey = GetUserKey("CurrentPlan");
            string expiryKey = GetUserKey("PlanExpiry");

            Response.Cookies.Delete(planKey);
            Response.Cookies.Delete(expiryKey);

            TempData["SuccessMessage"] = "Successfully switched back to the Free plan.";
            return RedirectToAction("Index");
        }

        public IActionResult ContactSales()
        {
            return View();
        }
    }
}