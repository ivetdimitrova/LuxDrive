using Microsoft.AspNetCore.Mvc;

namespace LuxDrive.Controllers
{
    public class FeaturesController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
