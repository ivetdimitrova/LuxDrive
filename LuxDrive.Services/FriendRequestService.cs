using LuxDrive.Data;
using LuxDrive.Data.Models;
using LuxDrive.Data.Models.Enums;
using LuxDrive.Services.Interfaces;
using LuxDrive.ViewModels.Friends;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LuxDrive.Services
{
    public class FriendRequestService : IFriendRequestService
    {
        private readonly LuxDriveDbContext _context;

        public FriendRequestService(LuxDriveDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ReceivedRequestViewModel>> GetReceivedRequestAsync(Guid userId)
        {
            return await _context.FriendRequests
               .Include(r => r.Sender)
               .AsNoTracking()
               .Where(r => r.ReceiverId == userId && r.Status == FriendRequestStatus.Pending)
               .Select(r => new ReceivedRequestViewModel
               {
                   Id = r.Id,
                   SenderName = r.Sender.UserName,
               })
               .ToListAsync();
        }

        public async Task<IEnumerable<UserSentRequestVIewModel>> GetSentRequestAsync(Guid userId)
        {
            return await _context.FriendRequests
                .Include(r => r.Receiver)
                .AsNoTracking()
                .Where(r => r.SenderId == userId && r.Status == FriendRequestStatus.Pending)
                .Select(r => new UserSentRequestVIewModel
                {
                    Id = r.Id,
                    ReceiverName = r.Receiver.UserName,
                })
                .ToListAsync();
        }

        public async Task SendRequestAsync(Guid senderId, string receiverEmail)
        {
            var receiver = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == receiverEmail);

            if (receiver == null) throw new InvalidOperationException("The user with the provided email does not exist.");

            if (senderId == receiver.Id) throw new InvalidOperationException("You cannot send an invitation to yourself..");

            bool exists = await _context.FriendRequests
                .AnyAsync(x => x.SenderId == senderId && x.ReceiverId == receiver.Id && x.Status == FriendRequestStatus.Pending);

            if (exists) return;

            var request = new FriendRequest
            {
                SenderId = senderId,
                ReceiverId = receiver.Id,
                Status = FriendRequestStatus.Pending,
                CreatedOn = DateTime.UtcNow
            };

            _context.FriendRequests.Add(request);
            await _context.SaveChangesAsync();
        }

        public async Task AcceptRequestAsync(Guid requestId)
        {
            //Todo: проверка дли съществува това приятелство
            var request = await _context.FriendRequests
                .FirstOrDefaultAsync(x => x.Id == requestId);

            if (request == null || request.Status != FriendRequestStatus.Pending)
                throw new InvalidOperationException("Invitation not found or not active.");

            request.Status = FriendRequestStatus.Accepted;


            //Todo: remove one recording
            var friendship1 = new UserFriend { UserId = request.SenderId, FriendId = request.ReceiverId };
            var friendship2 = new UserFriend { UserId = request.ReceiverId, FriendId = request.SenderId };

            await _context.UserFriends.AddRangeAsync(friendship1, friendship2);
            await _context.SaveChangesAsync();
        }
    }
}
