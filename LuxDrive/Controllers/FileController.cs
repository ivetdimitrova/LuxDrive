using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LuxDrive.Controllers
{
    [Authorize]
    public class FileController : BaseController
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
