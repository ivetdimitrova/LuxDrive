using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace LuxDrive.Controllers
{
    public class PricingController : Controller
    {
        [AllowAnonymous]
        public IActionResult Index()
        {
            return View();
        }

        [Authorize]
        public IActionResult Checkout(string plan)
        {
            ViewBag.Plan = plan;
            return View();
        }

        [Authorize]
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