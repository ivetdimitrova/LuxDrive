using LuxDrive.Data;
using LuxDrive.Data.Models;
using LuxDrive.Data.Models.Enums;
using LuxDrive.Services.Interfaces;
using LuxDrive.ViewModels.Friends;
using Microsoft.EntityFrameworkCore;

namespace LuxDrive.Services
{
    public class FriendRequestService : IFriendRequestService
    {
        private readonly LuxDriveDbContext _context;

        public FriendRequestService(LuxDriveDbContext context)
        {
            _context = context;
        }

        /*
        <summary>
        Метод за извличане на всички получени покани за приятелство, които чакат одобрение.
        Намира заявките, адресирани до конкретния потребител, и връща информация за изпращача (име и профилна снимка).
        </summary>
        <param name="userId">Id-то на потребителя, който проверява получените си покани.</param>
        <returns>Списък с получените покани за приятелство.</returns>
        <exception cref="ArgumentException">Гърми, ако Id-то на потребителя е невалидно.</exception>
        */
        public async Task<IEnumerable<ReceivedRequestViewModel>> GetReceivedRequestsAsync(string userId)
        {
            if (!Guid.TryParse(userId, out Guid userGuid)) 
                throw new ArgumentException("Invalid user id!");

            return await _context.FriendRequests
               .Where(r => r.ReceiverId == userGuid && r.Status == FriendRequestStatus.Pending)
               .AsNoTracking()
               .Select(r => new ReceivedRequestViewModel
               {
                   Id = r.Id,
                   SenderName = r.Sender.UserName,
                   ProfileImageUrl = r.Sender.ProfileImagePath
               })
               .ToListAsync();
        }

        /*
        <summary>
        Метод за извличане на всички изпратени покани за приятелство, които още не са приети.
        Показва на потребителя към кого е изпратил заявки, които все още са със статус "Pending".
        </summary>
        <param name="userId">Id-то на потребителя, който е изпратил поканите.</param>
        <returns>Списък с изпратените, но все още неодобрени покани.</returns>
        <exception cref="ArgumentException">Гърми, ако Id-то на потребителя е невалидно.</exception>
        */
        public async Task<IEnumerable<UserSentRequestViewModel>> GetSentRequestsAsync(string userId)
        {
            if (!Guid.TryParse(userId, out Guid userGuid)) 
                throw new ArgumentException("Invalid user id!");

            return await _context.FriendRequests
                .Where(r => r.SenderId == userGuid && r.Status == FriendRequestStatus.Pending)
                .AsNoTracking()
                .Select(r => new UserSentRequestViewModel
                {
                    Id = r.Id,
                    ReceiverName = r.Receiver.UserName,
                    ProfileImageUrl = r.Receiver.ProfileImagePath
                })
                .ToListAsync();
        }

        /*
        <summary>
        Метод за изпращане на нова покана за приятелство чрез имейл на получателя.
        Проверява дали получателят съществува, дали потребителят не праща покана на себе си и дали вече няма активна покана между тях.
        </summary>
        <param name="senderId">Id-то на изпращача.</param>
        <param name="receiverEmail">Имейлът на човека, когото искаме да добавим.</param>
        <exception cref="InvalidOperationException">Гърми, ако имейлът не съществува, ако пращаш на себе си или поканата вече е налице.</exception>
        */
        public async Task SendRequestAsync(string senderId, string receiverEmail)
        {
            if (!Guid.TryParse(senderId, out Guid senderIdGuid))
                throw new ArgumentException("Invalid user id!");

            var receiver = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == receiverEmail);

            if (receiver == null) throw new InvalidOperationException("The user with the provided email does not exist.");

            if (senderIdGuid == receiver.Id) throw new InvalidOperationException("You cannot send an invitation to yourself..");

            bool exists = await _context.FriendRequests
                .AnyAsync(x => x.SenderId == senderIdGuid && x.ReceiverId == receiver.Id && x.Status == FriendRequestStatus.Pending);

            if (exists) return;

            var request = new FriendRequest
            {
                SenderId = senderIdGuid,
                ReceiverId = receiver.Id,
                Status = FriendRequestStatus.Pending,
                CreatedOn = DateTime.UtcNow
            };

            _context.FriendRequests.Add(request);
            await _context.SaveChangesAsync();
        }

        /*
        <summary>
        Метод за приемане на покана за приятелство.
        Променя статуса на поканата, създава нова връзка в таблицата с приятели и премахва заявката, тъй като вече е изпълнена.
        </summary>
        <param name="requestId">Уникалното Id на поканата, която се приема.</param>
        <exception cref="InvalidOperationException">Гърми, ако поканата не е намерена, не е активна или приятелството вече съществува.</exception>
        */
        public async Task AcceptRequestAsync(Guid requestId)
        {

            if (requestId == Guid.Empty)
                throw new ArgumentException("Invalid request id!");

            FriendRequest? request = await _context.FriendRequests
                .FirstOrDefaultAsync(x => x.Id == requestId); 

            if (request == null || request.Status != FriendRequestStatus.Pending)
                throw new InvalidOperationException("Invitation not found or not active.");


            var friendship1 = await _context.UserFriends
                 .FirstOrDefaultAsync(x => 
                 (x.UserId == request.SenderId && x.FriendId == request.ReceiverId) || 
                 (x.UserId == request.ReceiverId && x.FriendId == request.SenderId));

            if (friendship1 != null)
                throw new ArgumentException("Such a friendship exists");


            request.Status = FriendRequestStatus.Accepted;

            var friendship = new UserFriend { UserId = request.SenderId, FriendId = request.ReceiverId };

          

            await _context.UserFriends.AddAsync(friendship);

             _context.Remove(request);
            await _context.SaveChangesAsync();
        }

        /*
        /// <summary>
        /// Метод за отхвърляне или изтриване на покана за приятелство.
        /// Намира поканата по нейното Id и я премахва окончателно от базата данни.
        /// </summary>
        /// <param name="requestId">Уникалното Id на поканата, която трябва да бъде изтрита.</param>
        /// <exception cref="ArgumentException">Гърми, ако поканата не е намерена в системата.</exception>
        */
        public async Task RejectRequestAsync(Guid requestId)
        {
            if (requestId == Guid.Empty)
                throw new ArgumentException("Invalid request id!");
            var request = await _context.FriendRequests.FindAsync(requestId);

            if (request != null)
            {
                _context.FriendRequests.Remove(request);
                await _context.SaveChangesAsync();
            }
            else
            {
                throw new ArgumentException("Request not found.");
            }
        }
    }
}
