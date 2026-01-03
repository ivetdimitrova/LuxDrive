using LuxDrive.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LuxDrive.Controllers
{
    [ApiController]
    [Route("api/friends")]
    [Authorize]
    public class FriendsController : ControllerBase
    {
        private readonly IFriendService _friendService;
        private readonly IFileService _fileService; 

        public FriendsController(IFriendService friendService, IFileService fileService)
        {
            _friendService = friendService;
            _fileService = fileService;
        }

        private Guid CurrentUserId
        {
            get
            {
                var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(id)) throw new UnauthorizedAccessException();
                return Guid.Parse(id);
            }
        }

        [HttpPost("request")]
        public async Task<IActionResult> Send(Guid receiverId)
        {
            try
            {
                await _friendService.SendRequestAsync(CurrentUserId, receiverId);
                return Ok();
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpPost("accept")]
        public async Task<IActionResult> Accept(int requestId)
        {
            try
            {
                await _friendService.AcceptRequestAsync(requestId);
                return Ok();
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingRequests()
        {
            var requests = await _friendService.GetPendingRequestsAsync(CurrentUserId);
            return Ok(requests);
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchUser(string email)
        {
            var user = await _friendService.FindUserByEmailAsync(email);
            if (user == null) return NotFound("Няма такъв потребител.");
            return Ok(new { id = user.Id, username = user.UserName, email = user.Email });
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetFriends()
        {
            var friends = await _friendService.GetFriendsAsync(CurrentUserId);
            return Ok(friends);
        }

        [HttpPost("share")]
        public async Task<IActionResult> ShareFile(Guid fileId, Guid receiverId)
        {
            try
            {
                await _fileService.ShareFileAsync(fileId, CurrentUserId, receiverId);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest("Грешка при споделяне: " + ex.Message);
            }
        }
        [HttpPost("remove")]
        public async Task<IActionResult> RemoveFriend(Guid friendId)
        {
            try
            {
                await _friendService.RemoveFriendAsync(CurrentUserId, friendId);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest("Грешка при премахване: " + ex.Message);
            }
        }
        [HttpGet("sent")]
        public async Task<IActionResult> GetSentRequests()
        {
            var requests = await _friendService.GetSentPendingRequestsAsync(CurrentUserId);
            return Ok(requests);
        }
    }
}