using Microsoft.AspNetCore.Mvc;

namespace LuxDrive.Controllers
{
    public class FilesController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
