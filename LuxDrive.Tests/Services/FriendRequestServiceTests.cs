using NUnit.Framework;
using Microsoft.EntityFrameworkCore;
using LuxDrive.Data;
using LuxDrive.Data.Models;
using LuxDrive.Data.Models.Enums;
using LuxDrive.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace LuxDrive.Tests.Services
{
    [TestFixture]
    public class FriendRequestServiceTests
    {
        private LuxDriveDbContext _dbContext;
        private FriendRequestService _service;

        [SetUp]
        public void Setup()
        {
            // Използваме InMemory база данни с уникално име (Guid), 
            // за да гарантираме, че всеки тест работи в напълно изолирана среда.
            var options = new DbContextOptionsBuilder<LuxDriveDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _dbContext = new LuxDriveDbContext(options);
            _service = new FriendRequestService(_dbContext);
        }

        [TearDown]
        public void TearDown()
        {
            // Изтриваме базата след всеки тест, за да не се натрупват "боклуци" в паметта.
            _dbContext.Database.EnsureDeleted();
            _dbContext.Dispose();
        }

        /// <summary>
        /// Тества успешното изпращане на покана за приятелство по имейл.
        /// Уверява се, че се създава запис в базата със статус Pending.
        /// </summary>
        [Test]
        public async Task SendRequestAsync_WithValidData_CreatesNewPendingRequest()
        {
            // Arrange
            var senderId = Guid.NewGuid();
            var receiver = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = "receiver@test.com",
                UserName = "Receiver",
                FirstName = "Ivan",
                LastName = "Ivanov"
            };

            _dbContext.Users.Add(receiver);
            await _dbContext.SaveChangesAsync();

            // Act 
            await _service.SendRequestAsync(senderId.ToString(), "receiver@test.com");

            // Assert 
            var request = await _dbContext.FriendRequests.FirstOrDefaultAsync();

            Assert.IsNotNull(request);
            Assert.AreEqual(senderId, request.SenderId);
            Assert.AreEqual(receiver.Id, request.ReceiverId);
            Assert.AreEqual(FriendRequestStatus.Pending, request.Status);
        }

        /// <summary>
        /// Тества дали сървисът хвърля правилната грешка, ако се опитаме да пратим покана на несъществуващ имейл.
        /// </summary>
        [Test]
        public void SendRequestAsync_ReceiverDoesNotExist_ThrowsInvalidOperationException()
        {
            // Arrange
            var senderId = Guid.NewGuid();
            var fakeEmail = "notfound@test.com";

            // Act & Assert 
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await _service.SendRequestAsync(senderId.ToString(), fakeEmail));

            Assert.AreEqual("The user with the provided email does not exist.", ex.Message);
        }

        /// <summary>
        /// Тества защитата срещу изпращане на покана за приятелство на самия себе си.
        /// </summary>
        [Test]
        public async Task SendRequestAsync_SendingToSelf_ThrowsInvalidOperationException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new ApplicationUser
            {
                Id = userId,
                Email = "myself@test.com",
                FirstName = "Ivan",
                LastName = "Ivanov"
            };

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            // Act & Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await _service.SendRequestAsync(userId.ToString(), "myself@test.com"));

            Assert.AreEqual("You cannot send an invitation to yourself..", ex.Message);
        }

        /// <summary>
        /// Тества приемането на покана. Според новата логика, методът създава запис 
        /// в таблицата UserFriends и след това ИЗТРИВА самата покана от FriendRequests.
        /// </summary>
        [Test]
        public async Task AcceptRequestAsync_ValidRequest_CreatesFriendshipAndRemovesRequest()
        {
            // Arrange
            var requestId = Guid.NewGuid();
            var senderId = Guid.NewGuid();
            var receiverId = Guid.NewGuid();

            var request = new FriendRequest
            {
                Id = requestId,
                SenderId = senderId,
                ReceiverId = receiverId,
                Status = FriendRequestStatus.Pending,
                CreatedOn = DateTime.UtcNow
            };

            _dbContext.FriendRequests.Add(request);
            await _dbContext.SaveChangesAsync();

            // Act
            await _service.AcceptRequestAsync(requestId);

            // Assert
            // Проверяваме дали поканата е изтрита от базата 
            var deletedRequest = await _dbContext.FriendRequests.FindAsync(requestId);
            Assert.IsNull(deletedRequest);

            // Проверяваме дали е създадено приятелството
            var friendships = await _dbContext.UserFriends.ToListAsync();
            Assert.AreEqual(1, friendships.Count);
            Assert.IsTrue(friendships.Any(f => f.UserId == senderId && f.FriendId == receiverId));
        }

        /// <summary>
        /// Тества дали сървисът хвърля грешка, ако се опитаме да приемем несъществуваща или вече приета покана.
        /// </summary>
        [Test]
        public void AcceptRequestAsync_RequestNotFoundOrNotPending_ThrowsException()
        {
            // Act & Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await _service.AcceptRequestAsync(Guid.NewGuid()));

            Assert.AreEqual("Invitation not found or not active.", ex.Message);
        }

        /// <summary>
        /// Тества изтриването (отхвърлянето) на покана за приятелство. Трябва да я премахне от базата.
        /// </summary>
        [Test]
        public async Task RejectRequestAsync_ValidRequest_RemovesRequestFromDatabase()
        {
            // Arrange
            var requestId = Guid.NewGuid();
            var request = new FriendRequest
            {
                Id = requestId,
                SenderId = Guid.NewGuid(),
                ReceiverId = Guid.NewGuid(),
                Status = FriendRequestStatus.Pending
            };

            _dbContext.FriendRequests.Add(request);
            await _dbContext.SaveChangesAsync();

            // Act
            await _service.RejectRequestAsync(requestId);

            // Assert
            var deletedRequest = await _dbContext.FriendRequests.FindAsync(requestId);
            Assert.IsNull(deletedRequest);
        }

        /// <summary>
        /// Тества извличането на входящите покани за приятелство. 
        /// Трябва да върне само тези, които са със статус Pending.
        /// </summary>
        [Test]
        public async Task GetReceivedRequestsAsync_ReturnsOnlyPendingRequestsForUser()
        {
            // Arrange
            var receiverId = Guid.NewGuid();
            var sender1 = new ApplicationUser { Id = Guid.NewGuid(), UserName = "User1", Email = "1@test.com", FirstName = "A", LastName = "B" };
            var sender2 = new ApplicationUser { Id = Guid.NewGuid(), UserName = "User2", Email = "2@test.com", FirstName = "C", LastName = "D" };

            _dbContext.FriendRequests.Add(new FriendRequest { Id = Guid.NewGuid(), ReceiverId = receiverId, SenderId = sender1.Id, Sender = sender1, Status = FriendRequestStatus.Pending });
            _dbContext.FriendRequests.Add(new FriendRequest { Id = Guid.NewGuid(), ReceiverId = receiverId, SenderId = sender2.Id, Sender = sender2, Status = FriendRequestStatus.Accepted });

            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _service.GetReceivedRequestsAsync(receiverId.ToString());

            // Assert
            var requests = result.ToList();
            Assert.AreEqual(1, requests.Count);
            Assert.AreEqual("User1", requests[0].SenderName);
        }

        /// <summary>
        /// Тества извличането на изходящите покани за приятелство.
        /// Трябва да върне списък на хората, на които потребителят е пратил покана (със статус Pending).
        /// </summary>
        [Test]
        public async Task GetSentRequestsAsync_ReturnsOnlyPendingRequestsSentByUser()
        {
            // Arrange
            var senderId = Guid.NewGuid();
            var receiver1 = new ApplicationUser { Id = Guid.NewGuid(), UserName = "Receiver1", Email = "1@test.com", FirstName = "A", LastName = "B" };
            var receiver2 = new ApplicationUser { Id = Guid.NewGuid(), UserName = "Receiver2", Email = "2@test.com", FirstName = "C", LastName = "D" };

            _dbContext.FriendRequests.Add(new FriendRequest { Id = Guid.NewGuid(), SenderId = senderId, ReceiverId = receiver1.Id, Receiver = receiver1, Status = FriendRequestStatus.Pending });
            _dbContext.FriendRequests.Add(new FriendRequest { Id = Guid.NewGuid(), SenderId = senderId, ReceiverId = receiver2.Id, Receiver = receiver2, Status = FriendRequestStatus.Accepted });

            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _service.GetSentRequestsAsync(senderId.ToString());

            // Assert
            var requests = result.ToList();
            Assert.AreEqual(1, requests.Count);
            Assert.AreEqual("Receiver1", requests[0].ReceiverName);
        }
    }
}