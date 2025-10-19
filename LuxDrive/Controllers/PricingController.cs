using Microsoft.AspNetCore.Mvc;

namespace LuxDrive.Controllers
{
    public class PricingController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
