using LuxDrive.Data;
using LuxDrive.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LuxDrive.Services
{
    public class PaymentCardService : IPaymentCardService
    {

        private readonly LuxDriveDbContext _context;

        public PaymentCardService(LuxDriveDbContext context)
        {
            _context = context;
        }

        public async Task<bool> HasUserLinkedCardAsync(string userId)
        {
            if (!Guid.TryParse(userId, out Guid userGuid)) return false;

            return await _context.PaymentCards.AnyAsync(c => c.UserId == userGuid);
        }
    }
}
