using LuxDrive.Data;
using LuxDrive.Data.Models;
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

        public async Task<ApplicationUser?> FindUserByEmailAsync(string email)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<IEnumerable<FriendViewModel>> GetFriendsAsync(Guid userId)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("Invalid user id!");

            return await _context.UserFriends
                .Where(uf => uf.UserId == userId||uf.FriendId==userId)
                .Include(uf => uf.Friend)
                .AsNoTracking()
                .Select(uf => new FriendViewModel
                {

                    Id = uf.UserId == userId ? uf.FriendId : uf.UserId,

                    Name = uf.UserId == userId
                ? $"{uf.Friend.FirstName} {uf.Friend.LastName}"
                : $"{uf.User.FirstName} {uf.User.LastName}",

                    Email = uf.UserId == userId ? uf.Friend.Email : uf.User.Email,

                    ProfileImageUrl = uf.UserId == userId
                ? uf.Friend.ProfileImagePath
                : uf.User.ProfileImagePath

                })
                .ToListAsync();
        }

        public async Task RemoveFriendAsync(string userId, Guid friendId)
        {
            if (!Guid.TryParse(userId, out Guid userGuid))
                throw new ArgumentException("Invalid user id!");


            var friendship = await _context.UserFriends
                .FirstOrDefaultAsync(x => (x.UserId == userGuid && x.FriendId == friendId) || (x.UserId == friendId && x.FriendId == userGuid));

            if (friendship != null) _context.UserFriends.Remove(friendship);

            await _context.SaveChangesAsync();
        }

 
    }
}