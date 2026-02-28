using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LuxDrive.Controllers
{
    public class BaseController : Controller
    {
        /// <summary>
        /// Метод за проверка на текущият потребител дали е влязъл в профила си (автентикиран).
        /// Използва се за бърза проверка на идентичността, преди да се изпълнят действия, изискващи оторизация.
        /// </summary>
        /// <returns>Връща true, ако потребителят е разпознат от системата, иначе false.</returns>
        protected bool IsUserAuthenticated()
        {
            bool retRes = false;

            if (this.User.Identity != null)
            {
                retRes = this.User.Identity.IsAuthenticated;
            }

            return retRes;
        }

        /// <summary>
        /// Метод за извличане на уникалния идентификатор (Id) на потребителя.
        /// Първо проверява дали потребителят е логнат и след това извлича неговото Id от Claims (твърденията) на сесията.
        /// </summary>
        /// <returns>Връща Id-то като текст или null, ако потребителят не е автентикиран.</returns>
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
