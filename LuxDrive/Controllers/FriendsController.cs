using LuxDrive.Services.Interfaces;
using LuxDrive.ViewModels.Friends;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LuxDrive.Controllers
{
    [ApiController]
    [Route("api/friends")]
    [Authorize]
    public class FriendsController : BaseController
    {
        private readonly IFriendService _friendService;
        private readonly IFileService _fileService;
        private readonly IFriendRequestService _friendRequestService;

        public FriendsController(IFriendService friendService, IFileService fileService, IFriendRequestService friendRequestService)
        {
            _friendService = friendService;
            _fileService = fileService;
            _friendRequestService = friendRequestService;
        }

        /// <summary>
        /// Метод за изпращане на покана за приятелство чрез имейл адрес.
        /// Идентифицира подателя и използва услугата за заявки, за да създаде нова покана към потребителя с посочения имейл.
        /// След иницииране на заявката, потребителят се пренасочва обратно към файловия мениджър.
        /// </summary>
        /// <param name="receiverEmail">Имейл адресът на потребителя, до когото се изпраща поканата.</param>
        /// <returns>Пренасочва към Index на FileController или връща BadRequest при грешка.</returns>
        [HttpPost]
        public async Task<IActionResult> Send([FromForm] string receiverEmail)
        {
            try
            {
                string? userIdStr = GetUserId();
                if (userIdStr == null) return Unauthorized();

                await _friendRequestService.SendRequestAsync(userIdStr, receiverEmail);
                return RedirectToAction("Index", "File");
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }


        /// <summary>
        /// Метод за приемане на покана за приятелство.
        ///  След успешно изпълнение пренасочва потребителя към списъка с файлове с потвърждаващо съобщение.
        /// </summary>
        /// <param name="requestId">Уникалният идентификатор на поканата, която се приема.</param>
        /// <returns>Пренасочва към Index страницата на FileController или връщаBadRequest при грешка.</returns>
        [HttpPost("friends/accept-request")]
        public async Task<IActionResult> Accept([FromForm] Guid requestId)
        {
            try
            {
                await _friendRequestService.AcceptRequestAsync(requestId);
                TempData["AlertMessage"] = "The invitation was accepted.";
                return RedirectToAction("Index", "File");


            }
            catch (Exception ex)
            { return BadRequest(ex.Message); }
        }


        /// <summary>
        /// Метод за търсене на потребител в системата по неговия имейл адрес.
        /// При успех връща основни данни (Id, потребителско име и имейл) в JSON формат, 
        /// а при липса на съвпадение – съобщение, че потребителят не е открит.
        /// </summary>
        /// <param name="email">Имейл адресът, по който се извършва търсенето.</param>
        /// <returns>Връща статус 200 (Ok) с данните на потребителя, 404 (NotFound) или BadRequest при грешка.</returns>
        [HttpGet("search")]
        public async Task<IActionResult> SearchUser(string email)
        {
            try
            {
                var user = await _friendService.FindUserByEmailAsync(email);
                if (user == null) return NotFound("No such user.");
                return Ok(new { id = user.Id, username = user.UserName, email = user.Email });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }


        /// <summary>
        /// Метод за подготовка и визуализация на основния социален панел (Friends Modal).
        /// Агрегира данни от няколко услуги, за да предостави пълен изглед на списъка с приятели, 
        /// изпратените покани и получените заявки за приятелство.
        /// </summary>
        /// <returns>Връща частично изгледно съдържание (PartialView) с консолидиран модел за социални взаимодействия.</returns>
        [HttpGet]
        public async Task<IActionResult> LoadFriendList()
        {
            try
            {
                string? userIdStr = GetUserId();
                if (userIdStr == null) return Unauthorized();

                IEnumerable<FriendViewModel> friends = await _friendService.GetFriendsAsync(Guid.Parse(base.GetUserId()));
                IEnumerable<UserSentRequestViewModel>? sentRequests = await _friendRequestService.GetSentRequestsAsync(userIdStr);
                IEnumerable<ReceivedRequestViewModel>? receivedRequests = await _friendRequestService.GetReceivedRequestsAsync(userIdStr);

                FriendsMainViewModel model = new FriendsMainViewModel
                {
                    Friends = friends,
                    SentRequests = sentRequests,
                    ReceivedRequests = receivedRequests
                };
                return PartialView("_FriendsModalPartial", model);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }


        }


        /// <summary>
        /// Метод за подготовка и визуализация на модалния прозорец за споделяне на файл с приятел.
        /// Извлича списъка с приятели на текущия потребител и ги зарежда в модела за споделяне.
        /// </summary>
        /// <param name="fileId">Идентификаторът на файла, който потребителят желае да сподели.</param>
        /// <returns>Връща частично изгледно съдържание (PartialView), представляващо модалния диалог за споделяне.</returns>
        [HttpPost("load-share-list", Name = "LoadShareRoute")]
        public async Task<ActionResult> LoadShareList([FromForm] Guid fileId)
        {
            try
            {
                ShareWithFriendViewModel model = new ShareWithFriendViewModel
                {
                    FileId = fileId,
                    Friends = await _friendService.GetFriendsAsync(Guid.Parse(base.GetUserId()))
                };

                return PartialView("_ShareModal", model);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }


        /// <summary>
        /// Метод за споделяне на файл с друг потребител.
        /// Извлича идентификатора на подателя, проверява неговата оторизация и извиква услугата за споделяне.
        /// </summary>
        /// <param name="fileId">Уникалният идентификатор на файла, който ще бъде споделен.</param>
        /// <param name="receiverId">Уникалният идентификатор на потребителя, който ще получи достъп до файла.</param>
        /// <returns>Връща статус 200 (Ok) при успех или BadRequest с описание на грешката.</returns>
        [HttpPost("share")]
        public async Task<IActionResult> ShareFile(Guid fileId, Guid receiverId)
        {
            try
            {
                string? userIdStr = GetUserId();
                if (userIdStr == null) return Unauthorized();

                await _fileService.ShareFileAsync(fileId, userIdStr, receiverId);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest("Sharing error: " + ex.Message);
            }
        }


        /// <summary>
        /// Метод за премахване на потребител от списъка с приятели.
        /// Извлича идентификатора на текущия потребител и извиква услугата за изтриване на връзката с посочения приятел.
        /// При успех визуализира съобщение и пренасочва потребителя към основния файлов панел.
        /// </summary>
        /// <param name="friendId">Уникалният идентификатор на приятеля, който трябва да бъде премахнат.</param>
        /// <returns>Пренасочва към Index на FileController или връща BadRequest при възникнала грешка.</returns>
        [HttpPost("remove")]
        public async Task<IActionResult> RemoveFriend([FromForm] Guid friendId)
        {
            try
            {
                string? userIdStr = GetUserId();
                if (userIdStr == null) return Unauthorized();


                await _friendService.RemoveFriendAsync(userIdStr, friendId);


                TempData["AlertMessage"] = "Friend removed.";

                return RedirectToAction("Index", "File");

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }


        /// <summary>
        /// Метод за отхвърляне на покана за приятелство.
        /// След успешното отхвърляне, потребителят се пренасочва обратно към файловия мениджър с потвърждаващо съобщение.
        /// </summary>
        /// <param name="requestId">Уникалният идентификатор на поканата, която трябва да бъде отхвърлена.</param>
        /// <returns>Пренасочва към списъка с файлове или връща грешка при проблем с обработката.</returns>
        [HttpPost("reject")]
        public async Task<IActionResult> Reject([FromForm] Guid requestId)
        {
            try
            {
                await _friendRequestService.RejectRequestAsync(requestId);
                TempData["AlertMessage"] = "The invitation was rejected.";

                return RedirectToAction("Index", "File");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}