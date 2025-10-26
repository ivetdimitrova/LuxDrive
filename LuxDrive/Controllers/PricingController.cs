using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LuxDrive.Controllers
{
    [Authorize] 
    public class PaymentController : Controller
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
    }
}
