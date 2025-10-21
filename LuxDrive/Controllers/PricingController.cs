using Microsoft.AspNetCore.Mvc;

namespace LuxDrive.Controllers
{
    public class PricingController : BaseController
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
