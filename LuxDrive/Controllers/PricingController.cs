using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LuxDrive.Controllers
{
    public class PricingController : Controller
    {
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

            string safeUserName = User.Identity.Name
                                      .Replace("@", "_")
                                      .Replace(".", "_");

            return $"{baseKey}_{safeUserName}";
        }

        public IActionResult Index()
        {
            string currentPlan = "None";
            bool hasCard = false;

            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                string planKey = GetUserKey("CurrentPlan");
                string cardKey = GetUserKey("HasCard");

                currentPlan = Request.Cookies[planKey] ?? "None";
                hasCard = Request.Cookies[cardKey] == "true";
            }

            ViewBag.CurrentPlan = currentPlan;
            ViewBag.HasCard = hasCard;

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
            if (string.IsNullOrEmpty(cardNumber) || cardNumber.Replace(" ", "").Length < 15)
            {
                TempData["ErrorMessage"] = "Invalid card number.";
                return RedirectToAction("Checkout", new { plan = plan });
            }

            await Task.Delay(2000);

            string planKey = GetUserKey("CurrentPlan");
            string cardKey = GetUserKey("HasCard");

            CookieOptions option = new CookieOptions { Expires = DateTime.Now.AddDays(30) };

            Response.Cookies.Append(planKey, plan, option);
            Response.Cookies.Append(cardKey, "true", option);

            TempData["SuccessMessage"] = $"Successfully activated {plan} plan!";
            return RedirectToAction("Index");
        }

        [Authorize]
        public async Task<IActionResult> QuickPurchase(string plan)
        {
            string cardKey = GetUserKey("HasCard");
            string planKey = GetUserKey("CurrentPlan");

            if (Request.Cookies[cardKey] != "true")
            {
                return RedirectToAction("Checkout", new { plan = plan });
            }

            string oldPlan = Request.Cookies[planKey] ?? "None";
            int oldRank = GetPlanRank(oldPlan);
            int newRank = GetPlanRank(plan);

            await Task.Delay(1500);

            CookieOptions option = new CookieOptions { Expires = DateTime.Now.AddDays(30) };
            Response.Cookies.Append(planKey, plan, option);

            if (newRank < oldRank)
            {
                TempData["SuccessMessage"] = $"Successfully switched to {plan} plan.";
            }
            else
            {
                TempData["SuccessMessage"] = $"Successfully upgraded to {plan} plan!";
            }

            return RedirectToAction("Index");
        }

        [Authorize]
        public IActionResult Downgrade()
        {
            string planKey = GetUserKey("CurrentPlan");

            Response.Cookies.Delete(planKey);

            TempData["SuccessMessage"] = "Successfully switched back to the Free plan.";
            return RedirectToAction("Index");
        }

        public IActionResult ContactSales()
        {
            return View();
        }
    }
}