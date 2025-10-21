using Microsoft.AspNetCore.Mvc;

namespace LuxDrive.Controllers
{
    public class FilesController : BaseController
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
