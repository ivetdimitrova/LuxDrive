using LuxDrive.Data;
using LuxDrive.Data.Models;
using LuxDrive.ViewModels.Pricing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
                    hasCard = await _context.PaymentCards.AnyAsync(c => c.UserId == user.Id);

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
        [HttpGet]
        public IActionResult Checkout(string plan)
        {
            if (string.IsNullOrEmpty(plan)) return RedirectToAction(nameof(Index));

            var model = new CheckoutViewModel
            {
                PlanName = plan
            };

            return View(model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Process(CheckoutViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Checkout", model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            await Task.Delay(2000);

            try
            {
                string cleanNumber = model.CardNumber.Replace(" ", "").Trim();
                string last4 = cleanNumber.Substring(cleanNumber.Length - 4);

                string cardType = cleanNumber.StartsWith("4") ? "visa" :
                                 cleanNumber.StartsWith("5") ? "mastercard" :
                                 cleanNumber.StartsWith("3") ? "amex" : "unknown";

                bool exists = await _context.PaymentCards.AnyAsync(c => c.UserId == user.Id && c.CardLast4 == last4);

                if (!exists)
                {
                    var newCard = new PaymentCard
                    {
                        UserId = user.Id,
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

            Response.Cookies.Append(planKey, model.PlanName, option);
            Response.Cookies.Append(expiryKey, validUntil.ToString(), option);

            TempData["SuccessMessage"] = $"Successfully activated {model.PlanName} plan!";
            return RedirectToAction("Index");
        }

        [Authorize]
        public async Task<IActionResult> QuickPurchase(string plan)
        {
            var user = await _userManager.GetUserAsync(User);
            bool hasCardInDb = await _context.PaymentCards.AnyAsync(c => c.UserId == user.Id);

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


        [HttpGet]
        public IActionResult ContactSales()
        {
            ContactSalesViewModel model = new ContactSalesViewModel();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ContactSales(ContactSalesViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Ok(new { success = true });
            }

            return View(model);
        }
    }
}