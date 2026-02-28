using LuxDrive.Data;
using LuxDrive.Services.Interfaces;

namespace LuxDrive.Services
{
    public class ApplicationUserService : IApplicationUserService
    {

        private readonly LuxDriveDbContext _context;

        public ApplicationUserService(LuxDriveDbContext context)
        {
            _context = context;
        }

        /*
        <summary>
        Метод за окончателно изтриване на потребителски акаунт и всички свързани с него данни.
        Премахва систематично споделените файлове, приятелствата, изпратените покани, личните файлове и банковите карти на потребителя, за да не остават „сираци“ в базата данни след изтриването на профила.
        </summary>
        <param name="userId">Id-то на потребителя, който иска да изтрие акаунта си.</param>
        <exception cref="ArgumentException">Гърми, ако подаденото Id не е валидно.</exception>
        */
        public async Task DeleteAccountAsync(string userId)
        {
            if (!Guid.TryParse(userId, out Guid userGuid))
                throw new ArgumentException("User with this id doesn't exist.");

            var sharedFiles = _context.SharedFiles.Where(sf => sf.SenderId == userGuid || sf.ReceiverId == userGuid);
            _context.SharedFiles.RemoveRange(sharedFiles);

            var friendships = _context.UserFriends.Where(f => f.UserId == userGuid || f.FriendId == userGuid);
            _context.UserFriends.RemoveRange(friendships);

            var friendRequests = _context.FriendRequests.Where(fr => fr.SenderId == userGuid || fr.ReceiverId == userGuid);
            _context.FriendRequests.RemoveRange(friendRequests);

            var userFiles = _context.Files.Where(f => f.UserId == userGuid);
            _context.Files.RemoveRange(userFiles);

            var userCards = _context.PaymentCards.Where(c => c.UserId == userGuid);
            _context.PaymentCards.RemoveRange(userCards);

            await _context.SaveChangesAsync();
        }
    }
}
