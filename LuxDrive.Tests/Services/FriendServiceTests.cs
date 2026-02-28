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
            // Използваме InMemory база данни с уникално име (Guid), 
            // за да гарантираме, че всеки тест работи в напълно изолирана среда.
            var options = new DbContextOptionsBuilder<LuxDriveDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _dbContext = new LuxDriveDbContext(options);
            _service = new FriendService(_dbContext);
        }

        [TearDown]
        public void TearDown()
        {
            // Изчистваме и затваряме базата след всеки тест
            _dbContext.Database.EnsureDeleted();
            _dbContext.Dispose();
        }

        /// <summary>
        /// Тества дали методът намира правилно потребител по подаден имейл адрес.
        /// </summary>
        [Test]
        public async Task FindUserByEmailAsync_ExistingUser_ReturnsUser()
        {
            // Arrange (Подготовка)
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

            // Act (Изпълнение)
            var result = await _service.FindUserByEmailAsync(email);

            // Assert (Проверка)
            Assert.IsNotNull(result);
            Assert.AreEqual(email, result.Email);
        }

        /// <summary>
        /// Тества дали методът връща null, ако се подаде имейл, който не съществува в базата.
        /// </summary>
        [Test]
        public async Task FindUserByEmailAsync_NonExistingUser_ReturnsNull()
        {
            // Arrange
            var fakeEmail = "notfound@luxdrive.com";

            // Act
            var result = await _service.FindUserByEmailAsync(fakeEmail);

            // Assert
            Assert.IsNull(result);
        }

        /// <summary>
        /// Тества дали методът връща правилно списъка с приятели и форматира данните им 
        /// (име, имейл, профилна снимка) в нужния ViewModel.
        /// </summary>
        [Test]
        public async Task GetFriendsAsync_ReturnsCorrectViewModelData()
        {
            // Arrange
            // Създаваме ОСНОВНИЯ потребител
            var mainUser = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = "main@test.com",
                UserName = "MainUser",
                FirstName = "Main",
                LastName = "User"
            };

            // Създаваме ПРИЯТЕЛЯ
            var friendUser = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = "friend@test.com",
                UserName = "FriendUser",
                FirstName = "Ivan",
                LastName = "Ivanov",
                ProfileImagePath = "image.jpg"
            };

            // Добавяме И ДВАМАТА в базата
            _dbContext.Users.AddRange(mainUser, friendUser);

            // Записваме приятелството в базата, свързвайки двамата съществуващи потребители
            _dbContext.UserFriends.Add(new UserFriend
            {
                UserId = mainUser.Id,
                FriendId = friendUser.Id,
                User = mainUser,
                Friend = friendUser
            });

            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _service.GetFriendsAsync(mainUser.Id);
            var friend = result.FirstOrDefault();

            // Assert
            Assert.IsNotNull(friend, "Списъкът с приятели е празен, връзката не е намерена!");
            Assert.AreEqual(friendUser.Id, friend.Id);
            Assert.AreEqual("Ivan Ivanov", friend.Name);
            Assert.AreEqual("friend@test.com", friend.Email);
            Assert.AreEqual("image.jpg", friend.ProfileImageUrl);
        }

        /// <summary>
        /// Тества дали методът хвърля грешка, ако му бъде подадено празно Guid (Guid.Empty) за userId.
        /// </summary>
        [Test]
        public void GetFriendsAsync_EmptyUserId_ThrowsArgumentException()
        {
            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(async () =>
                await _service.GetFriendsAsync(Guid.Empty));

            Assert.AreEqual("Invalid user id!", ex.Message);
        }

        /// <summary>
        /// Тества премахването на приятелство от базата данни.
        /// </summary>
        [Test]
        public async Task RemoveFriendAsync_ValidUsers_RemovesRelationFromDatabase()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var friendId = Guid.NewGuid();

            // Създаваме връзка за приятелство
            _dbContext.UserFriends.Add(new UserFriend { UserId = userId, FriendId = friendId });
            await _dbContext.SaveChangesAsync();

            // Act
            await _service.RemoveFriendAsync(userId.ToString(), friendId);

            // Assert
            var relationsCount = await _dbContext.UserFriends.CountAsync();
            Assert.AreEqual(0, relationsCount);
        }

        /// <summary>
        /// Тества дали методът хвърля грешка при опит за премахване на приятел, използвайки невалидно User ID.
        /// </summary>
        [Test]
        public void RemoveFriendAsync_InvalidUserId_ThrowsArgumentException()
        {
            // Arrange
            var invalidUserId = "not-a-valid-guid";
            var friendId = Guid.NewGuid();

            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(async () =>
                await _service.RemoveFriendAsync(invalidUserId, friendId));

            Assert.AreEqual("Invalid user id!", ex.Message);
        }
    }
}