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

        /*
        <summary>
        Метод за търсене на потребител в системата по неговия имейл адрес.
        Използва се основно, за да се провери дали даден човек съществува, преди да му се изпрати покана за приятелство.
        </summary>
        <param name="email">Имейл адресът на търсения потребител.</param>
        <returns>Връща обекта на потребителя, ако е намерен, или null, ако не съществува.</returns>
        */
        public async Task<ApplicationUser?> FindUserByEmailAsync(string email)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        /*
        <summary>
        Метод за извличане на пълния списък с приятели на даден потребител.
        Методът проверява връзките в двете посоки (кой кого е добавил) и подготвя данните за визуализация – имена, имейл и профилна снимка.
        </summary>
        <param name="userId">Уникалното Id на потребителя, чиито приятели търсим.</param>
        <returns>Списък с модели, съдържащи информация за приятелите.</returns>
        <exception cref="ArgumentException">Гърми, ако Id-то на потребителя е празно.</exception>
        */
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

        /*
        <summary>
        Метод за прекратяване на приятелство между двама потребители.
        Намира записа в базата данни, независимо кой е инициаторът на приятелството, и го премахва окончателно.
        </summary>
        <param name="userId">Id-то на текущия потребител.</param>
        <param name="friendId">Id-то на приятеля, който трябва да бъде премахнат.</param>
        <exception cref="ArgumentException">Гърми, ако Id-то на потребителя не е валидно.</exception>
        са
        */
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