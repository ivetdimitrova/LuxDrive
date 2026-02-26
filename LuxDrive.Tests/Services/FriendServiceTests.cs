using NUnit.Framework;
using Microsoft.EntityFrameworkCore;
using LuxDrive.Data;
using LuxDrive.Data.Models;
using LuxDrive.Services;
using LuxDrive.ViewModels.Friends;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace LuxDrive.Tests.Services
{
    [TestFixture]
    public class FriendServiceTests
    {
        private LuxDriveDbContext _dbContext;
        private FriendService _service;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<LuxDriveDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _dbContext = new LuxDriveDbContext(options);
            _service = new FriendService(_dbContext);
        }

        [TearDown]
        public void TearDown()
        {
            _dbContext.Database.EnsureDeleted();
            _dbContext.Dispose();
        }

        [Test]
        public async Task RejectRequestAsync_ExistingRequest_RemovesItFromDatabase()
        {
            // Arrange
            var requestId = Guid.NewGuid();
            _dbContext.FriendRequests.Add(new FriendRequest { Id = requestId, SenderId = Guid.NewGuid(), ReceiverId = Guid.NewGuid() });
            await _dbContext.SaveChangesAsync();

            // Act
            await _service.RejectRequestAsync(requestId);

            // Assert
            var exists = await _dbContext.FriendRequests.AnyAsync(r => r.Id == requestId);
            Assert.IsFalse(exists);
        }

        [Test]
        public void RejectRequestAsync_NonExistentRequest_ThrowsException()
        {
            // Act & Assert
            var ex = Assert.ThrowsAsync<Exception>(async () =>
                await _service.RejectRequestAsync(Guid.NewGuid()));

            Assert.AreEqual("Request not found.", ex.Message);
        }

        [Test]
        public async Task FindUserByEmailAsync_ExistingUser_ReturnsUser()
        {
            // Arrange
            var email = "test@luxdrive.com";
            _dbContext.Users.Add(new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = email,
                UserName = email,
                FirstName = "Test",
                LastName = "User"
            });
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _service.FindUserByEmailAsync(email);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(email, result.Email);
        }

        [Test]
        public async Task GetFriendsAsync_ReturnsCorrectViewModelData()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var friendUser = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = "friend@test.com",
                FirstName = "Ivan",
                LastName = "Ivanov",
                ProfileImagePath = "image.jpg"
            };

            _dbContext.Users.Add(friendUser);
            _dbContext.UserFriends.Add(new UserFriend { UserId = userId, FriendId = friendUser.Id });
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _service.GetFriendsAsync(userId);
            var friend = result.FirstOrDefault();

            // Assert
            Assert.IsNotNull(friend);
            Assert.AreEqual(friendUser.Id, friend.Id);
            Assert.AreEqual("Ivan Ivanov", friend.Name); 
            Assert.AreEqual("image.jpg", friend.ProfileImageUrl);
        }

        [Test]
        public async Task RemoveFriendAsync_RemovesBothRelations()
        {
            // Arrange
            var user1 = Guid.NewGuid();
            var user2 = Guid.NewGuid();

            _dbContext.UserFriends.Add(new UserFriend { UserId = user1, FriendId = user2 });
            _dbContext.UserFriends.Add(new UserFriend { UserId = user2, FriendId = user1 });
            await _dbContext.SaveChangesAsync();

            // Act
            await _service.RemoveFriendAsync(user1, user2);

            // Assert
            var relationsCount = await _dbContext.UserFriends.CountAsync();
            Assert.AreEqual(0, relationsCount);
        }
    }
}