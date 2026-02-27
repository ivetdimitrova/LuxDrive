using LuxDrive.Data;
using LuxDrive.Data.Models;
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

        public async Task CreateCard(Guid userId, string last4, string cardType)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("Invalid user ID.");

            if (string.IsNullOrWhiteSpace(last4) || last4.Length != 4)
                throw new ArgumentException("Card digits must be exactly 4.");

            bool exists = await _context.PaymentCards
                .AnyAsync(c => c.UserId == userId && c.CardLast4 == last4);

            if (!exists)
            {
                var newCard = new PaymentCard
                {
                    UserId = userId,
                    CardLast4 = last4,
                    CardType = cardType
                };

                await _context.PaymentCards.AddAsync(newCard);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> HasUserLinkedCardAsync(Guid userId)
        {
            if (userId == Guid.Empty)
                throw new  ArgumentException("Invalid user id!");

            return await _context.PaymentCards.AnyAsync(c => c.UserId == userId);
        }
    }
}
