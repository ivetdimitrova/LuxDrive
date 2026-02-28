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

        /*
        <summary>
        Метод за добавяне на нова разплащателна карта към профила на потребителя.
        Проверява дали данните са валидни и дали същата карта (със същите последни 4 цифри) вече не е добавена, преди да запише новата информация в базата.
        </summary>
        <param name="userId">Id-то на потребителя, който добавя картата.</param>
        <param name="last4">Последните четири цифри на картата.</param>
        <param name="cardType">Типът на картата (напр. Visa, Mastercard).</param>
        <exception cref="ArgumentException">Гърми, ако Id-то е невалидно или цифрите на картата не са точно 4.</exception>
        */
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

        /*
        <summary>
        Метод за изтриване на конкретна разплащателна карта от профила.
        Намира картата по нейното Id и проверява дали тя наистина принадлежи на потребителя, преди да я премахне окончателно от базата данни.
        </summary>
        <param name="cardId">Уникалното Id на картата за изтриване.</param>
        <param name="userId">Id-то на потребителя под формата на текст.</param>
        <exception cref="ArgumentException">Гърми, ако Id-то на потребителя е грешно или картата не съществува.</exception>
        */
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

        /*
        <summary>
        Метод за извличане на списък с всички карти, свързани с профила на даден потребител.
        Връща само необходимата информация (Id, последни цифри и тип) за визуализация в настройките на профила.
        </summary>
        <param name="userId">Уникалното Id на потребителя.</param>
        <returns>Списък с модели на картите на потребителя.</returns>
        <exception cref="ArgumentException">Гърми, ако Id-то на потребителя е невалидно.</exception>
        */
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

        /*
        <summary>
        Метод за бърза проверка дали даден потребител има поне една свързана карта.
        Използва се често за логика, свързана с абонаменти или плащания, за да се разбере дали потребителят е настроил платежен метод.
        </summary>
        <param name="userId">Уникалното Id на потребителя.</param>
        <returns>Връща true, ако има поне една карта, иначе false.</returns>
        <exception cref="ArgumentException">Гърми, ако Id-то на потребителя е невалидно.</exception>
        */
        public async Task<bool> HasUserLinkedCardAsync(Guid userId)
        {
            if (userId == Guid.Empty)
                throw new  ArgumentException("Invalid user id!");

            return await _context.PaymentCards.AnyAsync(c => c.UserId == userId);
        }
    }
}
