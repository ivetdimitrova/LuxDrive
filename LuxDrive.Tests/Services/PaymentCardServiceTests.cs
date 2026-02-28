using NUnit.Framework;
using Microsoft.EntityFrameworkCore;
using LuxDrive.Data;
using LuxDrive.Data.Models;
using LuxDrive.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace LuxDrive.Tests.Services
{
    [TestFixture]
    public class PaymentCardServiceTests
    {
        private LuxDriveDbContext _dbContext;
        private PaymentCardService _service;

        [SetUp]
        public void Setup()
        {
            // Използваме in-memory база данни с уникално име за всеки тест
            var options = new DbContextOptionsBuilder<LuxDriveDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _dbContext = new LuxDriveDbContext(options);
            _service = new PaymentCardService(_dbContext);
        }

        [TearDown]
        public void TearDown()
        {
            _dbContext.Database.EnsureDeleted();
            _dbContext.Dispose();
        }

        /// <summary>
        /// Тества дали методът успешно добавя нова карта, когато данните са валидни.
        /// </summary>
        [Test]
        public async Task CreateCardAsync_ValidData_AddsCardToDatabase()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var last4 = "1234";
            var cardType = "Visa";

            // Act
            await _service.CreateCardAsync(userId, last4, cardType);

            // Assert
            var cards = await _dbContext.PaymentCards.ToListAsync();
            Assert.AreEqual(1, cards.Count);
            Assert.AreEqual(userId, cards[0].UserId);
            Assert.AreEqual(last4, cards[0].CardLast4);
            Assert.AreEqual(cardType, cards[0].CardType);
        }

        /// <summary>
        /// Тества дали методът хвърля грешка при невалидно (празно) потребителско ID.
        /// </summary>
        [Test]
        public void CreateCardAsync_EmptyUserId_ThrowsArgumentException()
        {
            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(async () =>
                await _service.CreateCardAsync(Guid.Empty, "1234", "Visa"));

            Assert.AreEqual("Invalid user ID.", ex.Message);
        }

        /// <summary>
        /// Тества дали методът хвърля грешка, ако последните цифри не са точно 4 на брой.
        /// </summary>
        [TestCase("123")] // По-малко от 4
        [TestCase("12345")] // Повече от 4
        [TestCase("")] // Празно
        [TestCase(null)] // Null
        public void CreateCardAsync_InvalidLast4_ThrowsArgumentException(string? invalidLast4) 
        {
            // Arrange
            var userId = Guid.NewGuid();

            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(async () =>
                await _service.CreateCardAsync(userId, invalidLast4, "Mastercard"));

            Assert.AreEqual("Card digits must be exactly 4.", ex.Message);
        }

        /// <summary>
        /// Тества защитата от добавяне на дублиращи се карти (със същите 4 цифри за същия потребител).
        /// </summary>
        [Test]
        public async Task CreateCardAsync_CardAlreadyExists_DoesNotAddDuplicate()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var existingCard = new PaymentCard { Id = Guid.NewGuid(), UserId = userId, CardLast4 = "9999", CardType = "Visa" };
            _dbContext.PaymentCards.Add(existingCard);
            await _dbContext.SaveChangesAsync();

            // Act
            // Опитваме да добавим карта със същите цифри
            await _service.CreateCardAsync(userId, "9999", "Visa");

            // Assert
            // Броят на картите трябва да остане 1
            var cardsCount = await _dbContext.PaymentCards.CountAsync();
            Assert.AreEqual(1, cardsCount);
        }

        /// <summary>
        /// Тества успешното изтриване на карта от базата данни.
        /// </summary>
        [Test]
        public async Task DeleteCardAsync_ValidCardAndUser_RemovesCard()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var cardId = Guid.NewGuid();
            var card = new PaymentCard { Id = cardId, UserId = userId, CardLast4 = "1111", CardType = "Visa" };

            _dbContext.PaymentCards.Add(card);
            await _dbContext.SaveChangesAsync();

            // Act
            await _service.DeleteCardAsync(cardId, userId.ToString());

            // Assert
            var exists = await _dbContext.PaymentCards.AnyAsync(c => c.Id == cardId);
            Assert.IsFalse(exists);
        }

        /// <summary>
        /// Тества дали методът хвърля грешка при невалиден (не-Guid) низ за User ID.
        /// </summary>
        [Test]
        public void DeleteCardAsync_InvalidUserIdString_ThrowsArgumentException()
        {
            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(async () =>
                await _service.DeleteCardAsync(Guid.NewGuid(), "invalid-string"));

            Assert.AreEqual("User with this id doesn't exist.", ex.Message);
        }

        /// <summary>
        /// Тества дали методът хвърля грешка при опит за триене на несъществуваща карта 
        /// (или карта, която принадлежи на друг потребител).
        /// </summary>
        [Test]
        public void DeleteCardAsync_CardNotFound_ThrowsArgumentException()
        {
            // Arrange
            var userId = Guid.NewGuid();

            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(async () =>
                await _service.DeleteCardAsync(Guid.NewGuid(), userId.ToString()));

            Assert.AreEqual("Card not found!", ex.Message);
        }

        /// <summary>
        /// Тества дали методът правилно извлича и мапва списъка с карти на потребителя.
        /// </summary>
        [Test]
        public async Task GetUserCardsAsync_ValidUserId_ReturnsCardViewModels()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _dbContext.PaymentCards.Add(new PaymentCard { Id = Guid.NewGuid(), UserId = userId, CardLast4 = "1234", CardType = "Visa" });
            _dbContext.PaymentCards.Add(new PaymentCard { Id = Guid.NewGuid(), UserId = userId, CardLast4 = "5678", CardType = "Mastercard" });

            // Добавяме карта на друг потребител, за да сме сигурни, че няма да бъде върната
            _dbContext.PaymentCards.Add(new PaymentCard { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), CardLast4 = "0000", CardType = "Amex" });

            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _service.GetUserCardsAsync(userId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Any(c => c.CardLast4 == "1234" && c.CardType == "Visa"));
            Assert.IsTrue(result.Any(c => c.CardLast4 == "5678" && c.CardType == "Mastercard"));
        }

        /// <summary>
        /// Тества дали хвърля грешка при празно User ID.
        /// </summary>
        [Test]
        public void GetUserCardsAsync_EmptyUserId_ThrowsArgumentException()
        {
            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(async () =>
                await _service.GetUserCardsAsync(Guid.Empty));

            Assert.AreEqual("Invalid user id!", ex.Message);
        }

        /// <summary>
        /// Тества дали връща true, когато потребителят има поне една запазена карта.
        /// </summary>
        [Test]
        public async Task HasUserLinkedCardAsync_UserHasCard_ReturnsTrue()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _dbContext.PaymentCards.Add(new PaymentCard { Id = Guid.NewGuid(), UserId = userId, CardLast4 = "4444", CardType = "Visa" });
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _service.HasUserLinkedCardAsync(userId);

            // Assert
            Assert.IsTrue(result);
        }

        /// <summary>
        /// Тества дали връща false, когато потребителят няма запазени карти.
        /// </summary>
        [Test]
        public async Task HasUserLinkedCardAsync_UserHasNoCards_ReturnsFalse()
        {
            // Arrange
            var userId = Guid.NewGuid();

            // Act
            var result = await _service.HasUserLinkedCardAsync(userId);

            // Assert
            Assert.IsFalse(result);
        }

        /// <summary>
        /// Тества дали хвърля грешка при празно User ID.
        /// </summary>
        [Test]
        public void HasUserLinkedCardAsync_EmptyUserId_ThrowsArgumentException()
        {
            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(async () =>
                await _service.HasUserLinkedCardAsync(Guid.Empty));

            Assert.AreEqual("Invalid user id!", ex.Message);
        }
    }
}