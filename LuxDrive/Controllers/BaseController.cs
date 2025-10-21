using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LuxDrive.Controllers
{
    public class BaseController : Controller
    {
        protected bool IsUserAuthenticated()
        {
            bool retRes = false;

            if (this.User.Identity != null)
            {
                retRes = this.User.Identity.IsAuthenticated;
            }

            return retRes;
        }
        protected string? GetUserId()
        {
            string? userId = null;

            if (this.IsUserAuthenticated())
            {
                userId = this.User.FindFirstValue(ClaimTypes.NameIdentifier);
            }

            return userId;
        }
    }
}
