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
       

        [HttpPost]
        public async Task<IActionResult> Send([FromForm] string receiverEmail)
        {
            string? userIdStr = GetUserId();
            if (userIdStr == null) return Unauthorized();

            try
            {
                await _friendRequestService.SendRequestAsync(userIdStr, receiverEmail);
                return RedirectToAction("Index","File");
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

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

        [HttpGet("search")]
        public async Task<IActionResult> SearchUser(string email)
        {
            var user = await _friendService.FindUserByEmailAsync(email);
            if (user == null) return NotFound("No such user.");
            return Ok(new { id = user.Id, username = user.UserName, email = user.Email });
        }

        [HttpGet]
        public async Task<IActionResult> LoadFriendList()
        {

            string? userIdStr = GetUserId();
            if (userIdStr == null) return Unauthorized();

            IEnumerable<FriendViewModel> friends = await _friendService.GetFriendsAsync(Guid.Parse(base.GetUserId()));
            IEnumerable<UserSentRequestViewModel>? sentRequests = await _friendRequestService.GetSentRequestAsync(userIdStr);
            IEnumerable<ReceivedRequestViewModel>? receivedRequests = await _friendRequestService.GetReceivedRequestAsync(userIdStr);

            FriendsMainViewModel model = new FriendsMainViewModel
            {
                Friends = friends,
                SentRequests = sentRequests,
                ReceivedRequests = receivedRequests
            };
            return PartialView("_FriendsModalPartial", model);
        }

        [HttpPost("load-share-list", Name = "LoadShareRoute")]
        public async Task<ActionResult> LoadShareList([FromForm]Guid fileId)
        {
            ShareWithFriendViewModel model = new ShareWithFriendViewModel
            {
                FileId = fileId,
                Friends = await _friendService.GetFriendsAsync(Guid.Parse(base.GetUserId()))
            };

            return PartialView("_ShareModal", model);
        }

        [HttpPost("share")]
        public async Task<IActionResult> ShareFile(Guid fileId, Guid receiverId)
        {
            string? userIdStr = GetUserId();
            if (userIdStr == null) return Unauthorized();

            try
            {
                await _fileService.ShareFileAsync(fileId, userIdStr, receiverId);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest("Sharing error: " + ex.Message);
            }
        }

        [HttpPost("remove")]
        public async Task<IActionResult> RemoveFriend([FromForm] Guid friendId)
        {
            string? userIdStr = GetUserId();
            if (userIdStr == null) return Unauthorized();

            try
            {
                bool isRemoved = await _friendService.RemoveFriendAsync(userIdStr, friendId);

                if(!isRemoved)
                {
                    TempData["AlertMessage"] = "An error occurred while deleting the friendship!";
                }
                TempData["AlertMessage"] = "Friend removed.";

                return RedirectToAction("Index", "File");
            }
            catch (Exception ex)
            {
                return BadRequest("Error removing: " + ex.Message);
            }
        }

        [HttpPost("reject")]
        public async Task<IActionResult> Reject([FromForm]Guid requestId)
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