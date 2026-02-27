using LuxDrive.Data;
using LuxDrive.Data.Models;
using LuxDrive.Services.Interfaces;
using LuxDrive.ViewModels.Settings;
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

        public async Task CreateCardAsync(Guid userId, string last4, string cardType)
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

        public async Task DeleteCardAsync(Guid cardId, string userId)
        {

            if (!Guid.TryParse(userId, out Guid userGuid))
                throw new ArgumentException("User with this id doesn't exist.");

            var card = await _context.PaymentCards
          .FirstOrDefaultAsync(c => c.Id == cardId && c.UserId == userGuid);

            if (card == null)
            {
                throw new ArgumentException("Card not found!");
            }
                _context.PaymentCards.Remove(card);
                await _context.SaveChangesAsync();

        }

        public async Task<List<CardViewModel>?> GetUserCardsAsync(Guid userId)
        {

            if(userId == Guid.Empty)
                throw new ArgumentException("Invalid user id!");

           return await _context.PaymentCards
                                      .Where(c => c.UserId == userId)
                                      .Select(c => new CardViewModel
                                      {
                                          Id = c.Id,
                                          CardLast4 = c.CardLast4,
                                          CardType = c.CardType
                                      })
                                      .ToListAsync();
        }

        public async Task<bool> HasUserLinkedCardAsync(Guid userId)
        {
            if (userId == Guid.Empty)
                throw new  ArgumentException("Invalid user id!");

            return await _context.PaymentCards.AnyAsync(c => c.UserId == userId);
        }
    }
}
