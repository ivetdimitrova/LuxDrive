using NUnit.Framework;
using Microsoft.EntityFrameworkCore;
using Moq;
using Microsoft.AspNetCore.Http;
using LuxDrive.Data;
using LuxDrive.Data.Models;
using LuxDrive.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using FileEntity = LuxDrive.Data.Models.File;

namespace LuxDrive.Tests.Services
{
    [TestFixture]
    public class FileServiceTests
    {
        private LuxDriveDbContext _dbContext;
        private FileService _service;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<LuxDriveDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _dbContext = new LuxDriveDbContext(options);
            _service = new FileService(_dbContext);
        }

        [TearDown]
        public void TearDown()
        {
            _dbContext.Database.EnsureDeleted();
            _dbContext.Dispose();
        }

        [Test]
        public async Task CreateFileAsync_WithValidData_SavesFileToDatabase()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("my-document.pdf");
            mockFile.Setup(f => f.Length).Returns(1024); 

            // Act
            var newFileId = await _service.CreateFileAsync(userId.ToString(), mockFile.Object);

            // Assert
            Assert.IsNotNull(newFileId);
            var savedFile = await _dbContext.Files.FindAsync(newFileId);

            Assert.IsNotNull(savedFile);
            Assert.AreEqual("my-document", savedFile.Name); 
            Assert.AreEqual(".pdf", savedFile.Extension);
            Assert.AreEqual(1024, savedFile.Size);
            Assert.AreEqual(userId, savedFile.UserId);
        }

        [Test]
        public async Task ChangeFileNameAsync_WhenGivenNameWithExtension_StripsExtensionAndSaves()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var file = new FileEntity
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Name = "old-name",
                Extension = ".jpg",
                StorageUrl = "url",
                UploadAt = DateTime.UtcNow
            };

            _dbContext.Files.Add(file);
            await _dbContext.SaveChangesAsync();

            // Act 
            var result = await _service.ChangeFileNameAsync(userId.ToString(), file.Id, "new-photo.jpg");

            // Assert
            Assert.IsTrue(result);
            var updatedFile = await _dbContext.Files.FindAsync(file.Id);

            Assert.AreEqual("new-photo", updatedFile.Name);
        }

        [Test]
        public void ShareFileAsync_UsersAreNotFriends_ThrowsInvalidOperationException()
        {
            // Arrange
            var fileId = Guid.NewGuid();
            var senderId = Guid.NewGuid();
            var receiverId = Guid.NewGuid();

            // Act & Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await _service.ShareFileAsync(fileId, senderId.ToString(), receiverId));

            Assert.AreEqual("Users are not friends.", ex.Message);
        }

        [Test]
        public async Task ShareFileAsync_UsersAreFriends_CreatesSharedFileRecord()
        {
            // Arrange
            var fileId = Guid.NewGuid();
            var senderId = Guid.NewGuid();
            var receiverId = Guid.NewGuid();

            _dbContext.UserFriends.Add(new UserFriend { UserId = senderId, FriendId = receiverId });
            await _dbContext.SaveChangesAsync();

            // Act
            await _service.ShareFileAsync(fileId, senderId.ToString(), receiverId);

            // Assert
            var sharedFile = await _dbContext.SharedFiles.FirstOrDefaultAsync();
            Assert.IsNotNull(sharedFile);
            Assert.AreEqual(fileId, sharedFile.FileId);
            Assert.AreEqual(senderId, sharedFile.SenderId);
            Assert.AreEqual(receiverId, sharedFile.ReceiverId);
        }

        [Test]
        public async Task GetUserFilesAsync_AssignsCorrectIconBasedOnExtension()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _dbContext.Files.Add(new FileEntity { Id = Guid.NewGuid(), UserId = userId, Name = "song", Extension = ".mp3", StorageUrl = "url", UploadAt = DateTime.UtcNow });
            _dbContext.Files.Add(new FileEntity { Id = Guid.NewGuid(), UserId = userId, Name = "doc", Extension = ".pdf", StorageUrl = "url", UploadAt = DateTime.UtcNow });
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _service.GetUserFilesAsync(userId.ToString());
            var filesList = result.ToList();

            // Assert
            Assert.AreEqual(2, filesList.Count);

            var mp3File = filesList.First(f => f.Extension == ".mp3");
            Assert.AreEqual("fas fa-music", mp3File.Icon);

            var pdfFile = filesList.First(f => f.Extension == ".pdf");
            Assert.AreEqual("fas fa-file-pdf", pdfFile.Icon);
        }

        [Test]
        public async Task GetFileExtensionAsync_ValidId_ReturnsExtension()
        {
            // Arrange
            var fileId = Guid.NewGuid();
            _dbContext.Files.Add(new FileEntity
            {
                Id = fileId,
                Name = "test",
                Extension = ".png",
                UserId = Guid.NewGuid(),
                StorageUrl = "url",
                UploadAt = DateTime.UtcNow
            });
            await _dbContext.SaveChangesAsync();

            // Act
            var extension = await _service.GetFileExtensionAsync(fileId);

            // Assert
            Assert.AreEqual(".png", extension);
        }

        [Test]
        public async Task UpdateFileUrlAsync_ValidId_UpdatesUrl()
        {
            // Arrange
            var fileId = Guid.NewGuid();
            var file = new FileEntity
            {
                Id = fileId,
                Name = "test",
                Extension = ".txt",
                UserId = Guid.NewGuid(),
                StorageUrl = "old-url",
                UploadAt = DateTime.UtcNow
            };
            _dbContext.Files.Add(file);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _service.UpdateFileUrlAsync(fileId, "new-cloud-url");

            // Assert
            Assert.IsTrue(result);
            var updatedFile = await _dbContext.Files.FindAsync(fileId);
            Assert.AreEqual("new-cloud-url", updatedFile.StorageUrl);
        }

        [Test]
        public async Task RemoveFileAsync_ValidFile_RemovesFromDatabase()
        {
            // Arrange
            var file = new FileEntity
            {
                Id = Guid.NewGuid(),
                Name = "to-delete",
                Extension = ".txt",
                UserId = Guid.NewGuid(),
                StorageUrl = "url",
                UploadAt = DateTime.UtcNow
            };
            _dbContext.Files.Add(file);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _service.RemoveFileAsync(file);

            // Assert
            Assert.IsTrue(result);
            var exists = await _dbContext.Files.AnyAsync(f => f.Id == file.Id);
            Assert.IsFalse(exists);
        }

        [Test]
        public async Task GetUserFileAsync_ValidUserAndFile_ReturnsFile()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var fileId = Guid.NewGuid();
            _dbContext.Files.Add(new FileEntity
            {
                Id = fileId,
                UserId = userId,
                Name = "my-file",
                Extension = ".zip",
                StorageUrl = "url",
                UploadAt = DateTime.UtcNow
            });
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _service.GetUserFileAsync(fileId, userId.ToString());

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("my-file", result.Name);
        }

        [Test]
        public async Task GetSharedWithMeFilesAsync_ReturnsFilesSharedWithUser()
        {
            // Arrange
            var receiverId = Guid.NewGuid();
            var sender = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = "SenderUser",
                Email = "sender@test.com",
                FirstName = "Test",
                LastName = "User"    
            };

            var file = new FileEntity
            {
                Id = Guid.NewGuid(),
                Name = "SharedDoc",
                Extension = ".pdf",
                UserId = sender.Id,
                StorageUrl = "url",
                UploadAt = DateTime.UtcNow
            };

            _dbContext.Users.Add(sender);
            _dbContext.Files.Add(file);

            _dbContext.SharedFiles.Add(new SharedFile
            {
                FileId = file.Id,
                SenderId = sender.Id,
                ReceiverId = receiverId,
                SharedOn = DateTime.UtcNow
            });

            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _service.GetSharedWithMeFilesAsync(receiverId.ToString());
            var list = result.ToList();

            // Assert
            Assert.AreEqual(1, list.Count);
            Assert.AreEqual("SharedDoc", list[0].Name);
            Assert.AreEqual("SenderUser", list[0].SenderName);
        }
    }
}