using NUnit.Framework;
using Microsoft.EntityFrameworkCore;
using LuxDrive.Data;
using LuxDrive.Data.Models;
using LuxDrive.Data.Models.Enums;
using LuxDrive.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using FileEntity = LuxDrive.Data.Models.File;

namespace LuxDrive.Tests.Services
{
    [TestFixture]
    public class ApplicationUserServiceTests
    {
        private LuxDriveDbContext _dbContext;
        private ApplicationUserService _service;

        [SetUp]
        public void Setup()
        {
            // Използваме in-memory база данни с уникално име (Guid) за всеки тест,
            // за да сме сигурни, че тестовете са изолирани един от друг.
            var options = new DbContextOptionsBuilder<LuxDriveDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _dbContext = new LuxDriveDbContext(options);
            _service = new ApplicationUserService(_dbContext);
        }

        [TearDown]
        public void TearDown()
        {
            // Изчистваме базата след всеки тест
            _dbContext.Database.EnsureDeleted();
            _dbContext.Dispose();
        }

        /// <summary>
        /// Тества дали методът хвърля грешка, ако му бъде подадено невалидно User ID (не-Guid низ).
        /// </summary>
        [Test]
        public void DeleteAccountAsync_InvalidUserId_ThrowsArgumentException()
        {
            // Arrange
            var invalidUserId = "invalid-guid-string";

            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(async () =>
                await _service.DeleteAccountAsync(invalidUserId));

            Assert.AreEqual("User with this id doesn't exist.", ex.Message);
        }

        /// <summary>
        /// Тества пълното изчистване на акаунта. 
        /// Създава множество свързани записи (файлове, карти, приятелства, покани) за двама потребители.
        /// Изтрива данните само на първия и проверява дали всичко негово е изчезнало, а данните на втория са непокътнати.
        /// </summary>
        [Test]
        public async Task DeleteAccountAsync_ValidUserId_RemovesAllAssociatedData()
        {
            // Arrange
            var userToDeleteId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid(); // Този потребител не трябва да бъде засегнат
            var thirdUserId = Guid.NewGuid(); // Използваме го за връзки (приятелства и т.н.)

            // 1. Споделени файлове (където потребителят е подател или получател)
            _dbContext.SharedFiles.Add(new SharedFile { FileId = Guid.NewGuid(), SenderId = userToDeleteId, ReceiverId = thirdUserId, SharedOn = DateTime.UtcNow });
            _dbContext.SharedFiles.Add(new SharedFile { FileId = Guid.NewGuid(), SenderId = thirdUserId, ReceiverId = userToDeleteId, SharedOn = DateTime.UtcNow });
            _dbContext.SharedFiles.Add(new SharedFile { FileId = Guid.NewGuid(), SenderId = otherUserId, ReceiverId = thirdUserId, SharedOn = DateTime.UtcNow }); // Този трябва да остане

            // 2. Приятелства (където потребителят е UserId или FriendId)
            _dbContext.UserFriends.Add(new UserFriend { UserId = userToDeleteId, FriendId = thirdUserId });
            _dbContext.UserFriends.Add(new UserFriend { UserId = thirdUserId, FriendId = userToDeleteId });
            _dbContext.UserFriends.Add(new UserFriend { UserId = otherUserId, FriendId = thirdUserId }); // Този трябва да остане

            // 3. Покани за приятелство (където потребителят е подател или получател)
            _dbContext.FriendRequests.Add(new FriendRequest { Id = Guid.NewGuid(), SenderId = userToDeleteId, ReceiverId = thirdUserId, Status = FriendRequestStatus.Pending });
            _dbContext.FriendRequests.Add(new FriendRequest { Id = Guid.NewGuid(), SenderId = thirdUserId, ReceiverId = userToDeleteId, Status = FriendRequestStatus.Pending });
            _dbContext.FriendRequests.Add(new FriendRequest { Id = Guid.NewGuid(), SenderId = otherUserId, ReceiverId = thirdUserId, Status = FriendRequestStatus.Pending }); // Този трябва да остане

            // 4. Лични файлове
            _dbContext.Files.Add(new FileEntity { Id = Guid.NewGuid(), UserId = userToDeleteId, Name = "MyFile", Extension = ".pdf", StorageUrl = "url1", UploadAt = DateTime.UtcNow });
            _dbContext.Files.Add(new FileEntity { Id = Guid.NewGuid(), UserId = otherUserId, Name = "OtherFile", Extension = ".txt", StorageUrl = "url2", UploadAt = DateTime.UtcNow }); // Този трябва да остане

            // 5. Банкови карти
            _dbContext.PaymentCards.Add(new PaymentCard { Id = Guid.NewGuid(), UserId = userToDeleteId, CardLast4 = "1234", CardType = "Visa" });
            _dbContext.PaymentCards.Add(new PaymentCard { Id = Guid.NewGuid(), UserId = otherUserId, CardLast4 = "5678", CardType = "Mastercard" }); // Този трябва да остане

            await _dbContext.SaveChangesAsync();

            // Act - Изпълняваме изтриването за основния потребител
            await _service.DeleteAccountAsync(userToDeleteId.ToString());

            // Assert - Проверяваме дали всички свързани данни с userToDeleteId са изтрити
            var sharedFiles = await _dbContext.SharedFiles.ToListAsync();
            Assert.AreEqual(1, sharedFiles.Count, "Трябва да остане само споделеният файл на другия потребител.");
            Assert.IsTrue(sharedFiles.All(sf => sf.SenderId != userToDeleteId && sf.ReceiverId != userToDeleteId));

            var friendships = await _dbContext.UserFriends.ToListAsync();
            Assert.AreEqual(1, friendships.Count, "Трябва да остане само приятелството на другия потребител.");
            Assert.IsTrue(friendships.All(f => f.UserId != userToDeleteId && f.FriendId != userToDeleteId));

            var friendRequests = await _dbContext.FriendRequests.ToListAsync();
            Assert.AreEqual(1, friendRequests.Count, "Трябва да остане само поканата на другия потребител.");
            Assert.IsTrue(friendRequests.All(fr => fr.SenderId != userToDeleteId && fr.ReceiverId != userToDeleteId));

            var files = await _dbContext.Files.ToListAsync();
            Assert.AreEqual(1, files.Count, "Трябва да остане само файлът на другия потребител.");
            Assert.IsTrue(files.All(f => f.UserId != userToDeleteId));

            var cards = await _dbContext.PaymentCards.ToListAsync();
            Assert.AreEqual(1, cards.Count, "Трябва да остане само картата на другия потребител.");
            Assert.IsTrue(cards.All(c => c.UserId != userToDeleteId));
        }
    }
}