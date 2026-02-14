using LuxDrive.Data;
using LuxDrive.Data.Models;
using LuxDrive.Data.Models.Enums;
using LuxDrive.Services.Interfaces;
using LuxDrive.ViewModels.Friends;
using Microsoft.EntityFrameworkCore;

namespace LuxDrive.Services
{
    public class FriendService : IFriendService
    {
        private readonly LuxDriveDbContext _context;

        public FriendService(LuxDriveDbContext context)
        {
            _context = context;
        }

   

        public async Task RejectRequestAsync(Guid requestId)
        {
            var request = await _context.FriendRequests.FindAsync(requestId);

            if (request != null)
            {
                _context.FriendRequests.Remove(request);
                await _context.SaveChangesAsync();
            }
            else
            {
                throw new Exception("Request not found.");
            }
        }

        public async Task<ApplicationUser?> FindUserByEmailAsync(string email)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<IEnumerable<FriendViewModel>> GetFriendsAsync(Guid userId)
        {
            return await _context.UserFriends
                .Where(uf => uf.UserId == userId)
                .Include(uf => uf.Friend)
                .AsNoTracking()
                .Select(uf => new FriendViewModel
                {
                    Id = uf.FriendId,
                    Email = uf.Friend.Email,
                    Name = $"{uf.Friend.FirstName} {uf.Friend.LastName}",
                    ProfileImageUrl = uf.Friend.ProfileImagePath

                })
                .ToListAsync();
        }

        public async Task RemoveFriendAsync(Guid userId, Guid friendId)
        {
            //TODO: remove 1 relation
            var friendship1 = await _context.UserFriends
                .FirstOrDefaultAsync(x => x.UserId == userId && x.FriendId == friendId);

            var friendship2 = await _context.UserFriends
                .FirstOrDefaultAsync(x => x.UserId == friendId && x.FriendId == userId);

            if (friendship1 != null) _context.UserFriends.Remove(friendship1);
            if (friendship2 != null) _context.UserFriends.Remove(friendship2);

            await _context.SaveChangesAsync();
        }

 
    }
}