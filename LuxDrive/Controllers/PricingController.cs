using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LuxDrive.Controllers
{
    [Authorize] 
    public class PricingController : Controller
    {
        public IActionResult Checkout(string plan)
        {
            ViewBag.Plan = plan;
            return View();
        }

        public IActionResult ContactSales()
        {
            return View();
        }

        [HttpPost]
        [Authorize]
        public IActionResult Process(string cardNumber, string expiry, string cvc)
        {
            ViewBag.Message = "Payment successful!";
            return View("Success");
        }
    }
}
