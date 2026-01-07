using LuxDrive.Data;
using LuxDrive.Data.Models;
using LuxDrive.Data.Models.Enums;
using LuxDrive.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LuxDrive.Services
{
    public class FriendService : IFriendService
    {
        private readonly LuxDriveDbContext _context;

        public FriendService(LuxDriveDbContext context)
        {
            _context = context;
        }

        public async Task SendRequestAsync(Guid senderId, Guid receiverId)
        {
            if (senderId == receiverId) throw new InvalidOperationException("Не може да пратите покана на себе си.");

            bool exists = await _context.FriendRequests
                .AnyAsync(x => x.SenderId == senderId && x.ReceiverId == receiverId && x.Status == FriendRequestStatus.Pending);

            if (exists) return; 

            var request = new FriendRequest
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Status = FriendRequestStatus.Pending,
                CreatedOn = DateTime.UtcNow
            };

            _context.FriendRequests.Add(request);
            await _context.SaveChangesAsync();
        }

        public async Task AcceptRequestAsync(int requestId)
        {
            var request = await _context.FriendRequests
                .FirstOrDefaultAsync(x => x.Id == requestId);

            if (request == null || request.Status != FriendRequestStatus.Pending)
                throw new InvalidOperationException("Поканата не е намерена или не е активна.");

            request.Status = FriendRequestStatus.Accepted;

            var friendship1 = new UserFriend { UserId = request.SenderId, FriendId = request.ReceiverId };
            var friendship2 = new UserFriend { UserId = request.ReceiverId, FriendId = request.SenderId };

            await _context.UserFriends.AddRangeAsync(friendship1, friendship2);
            await _context.SaveChangesAsync();
        }

        public async Task RejectRequestAsync(int requestId)
        {
            var request = await _context.FriendRequests.FindAsync(requestId);

            if (request != null)
            {
                _context.FriendRequests.Remove(request);
                await _context.SaveChangesAsync();
            }
            else
            {
                throw new Exception("Заявката не е намерена.");
            }
        }

        public async Task<IEnumerable<object>> GetPendingRequestsAsync(Guid userId)
        {
            return await _context.FriendRequests
                .Include(r => r.Sender) 
                .Where(r => r.ReceiverId == userId && r.Status == FriendRequestStatus.Pending)
                .Select(r => new
                {
                    Id = r.Id,                    
                    SenderName = r.Sender.UserName, 
                    SenderEmail = r.Sender.Email,   
                    SentOn = r.CreatedOn
                })
                .ToListAsync();
        }

        public async Task<ApplicationUser?> FindUserByEmailAsync(string email)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<IEnumerable<object>> GetFriendsAsync(Guid userId)
        {
            return await _context.UserFriends
                .Where(uf => uf.UserId == userId)
                .Select(uf => new
                {
                    Id = uf.FriendId,
                    Username = uf.Friend.UserName,
                    Email = uf.Friend.Email
                })
                .ToListAsync();
        }
        public async Task RemoveFriendAsync(Guid userId, Guid friendId)
        {
            var friendship1 = await _context.UserFriends
                .FirstOrDefaultAsync(x => x.UserId == userId && x.FriendId == friendId);

            var friendship2 = await _context.UserFriends
                .FirstOrDefaultAsync(x => x.UserId == friendId && x.FriendId == userId);

            if (friendship1 != null) _context.UserFriends.Remove(friendship1);
            if (friendship2 != null) _context.UserFriends.Remove(friendship2);

            await _context.SaveChangesAsync();
        }
        public async Task<IEnumerable<object>> GetSentPendingRequestsAsync(Guid userId)
        {
            return await _context.FriendRequests
                .Include(r => r.Receiver) 
                .Where(r => r.SenderId == userId && r.Status == FriendRequestStatus.Pending)
                .Select(r => new
                {
                    Id = r.Id,
                    ReceiverName = r.Receiver.UserName,
                    ReceiverEmail = r.Receiver.Email,
                    SentOn = r.CreatedOn
                })
                .ToListAsync();
        }
    }
}