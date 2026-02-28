using NUnit.Framework;
using Microsoft.EntityFrameworkCore;
using Moq;
using Microsoft.AspNetCore.Http;
using LuxDrive.Data;
using LuxDrive.Data.Models;
using LuxDrive.Services;
using LuxDrive.ViewModels.File;
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
            // Използваме InMemory база данни, за да изолираме тестовете.
            // Guid.NewGuid() гарантира, че всеки тест работи с чисто нова, празна база.
            var options = new DbContextOptionsBuilder<LuxDriveDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _dbContext = new LuxDriveDbContext(options);
            _service = new FileService(_dbContext);
        }

        [TearDown]
        public void TearDown()
        {
            // Изчистваме и затваряме базата след всеки тест
            _dbContext.Database.EnsureDeleted();
            _dbContext.Dispose();
        }

        /// <summary>
        /// Тества дали методът създава правилно запис в базата, когато му подадем валиден файл.
        /// </summary>
        [Test]
        public async Task CreateFileAsync_WithValidData_SavesFileToDatabase()
        {
            // Arrange (Подготовка)
            var userId = Guid.NewGuid();
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("my-document.pdf");
            mockFile.Setup(f => f.Length).Returns(1024);

            // Act (Изпълнение)
            var newFileId = await _service.CreateFileAsync(userId.ToString(), mockFile.Object);

            // Assert (Проверка)
            Assert.IsNotNull(newFileId);
            var savedFile = await _dbContext.Files.FindAsync(newFileId);

            Assert.IsNotNull(savedFile);
            Assert.AreEqual("my-document", savedFile.Name);
            Assert.AreEqual(".pdf", savedFile.Extension);
            Assert.AreEqual(1024, savedFile.Size);
            Assert.AreEqual(userId, savedFile.UserId);
        }

        /// <summary>
        /// Тества дали при опит за преименуване с разширение (напр. "pesho.jpg"),
        /// методът премахва разширението и запазва само името ("pesho").
        /// </summary>
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
            await _service.ChangeFileNameAsync(userId.ToString(), file.Id, "new-photo.jpg");

            // Assert
            var updatedFile = await _dbContext.Files.FindAsync(file.Id);
            Assert.AreEqual("new-photo", updatedFile.Name);
        }

        /// <summary>
        /// Тества дали методът за споделяне хвърля грешка, ако двамата потребители не са приятели.
        /// </summary>
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

        /// <summary>
        /// Тества успешно споделяне на файл, когато потребителите са свързани като приятели.
        /// </summary>
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

        /// <summary>
        /// Тества дали методът извлича само активните (неизтрити) файлове 
        /// и дали им задава правилната FontAwesome икона според разширението.
        /// </summary>
        [Test]
        public async Task GetUserFilesAsync_AssignsCorrectIconBasedOnExtension_AndIgnoresDeleted()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _dbContext.Files.Add(new FileEntity { Id = Guid.NewGuid(), UserId = userId, Name = "song", Extension = ".mp3", StorageUrl = "url", UploadAt = DateTime.UtcNow, IsDeleted = false });
            _dbContext.Files.Add(new FileEntity { Id = Guid.NewGuid(), UserId = userId, Name = "doc", Extension = ".pdf", StorageUrl = "url", UploadAt = DateTime.UtcNow, IsDeleted = false });
            _dbContext.Files.Add(new FileEntity { Id = Guid.NewGuid(), UserId = userId, Name = "del", Extension = ".png", StorageUrl = "url", UploadAt = DateTime.UtcNow, IsDeleted = true }); // Този не трябва да се връща
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

        /// <summary>
        /// Тества дали методът връща правилното разширение по дадено ID на файл.
        /// </summary>
        [Test]
        public async Task GetFileExtensionAsync_ValidId_ReturnsExtension()
        {
            // Arrange
            var fileId = Guid.NewGuid();
            _dbContext.Files.Add(new FileEntity { Id = fileId, Name = "test", Extension = ".png", UserId = Guid.NewGuid(), StorageUrl = "url", UploadAt = DateTime.UtcNow });
            await _dbContext.SaveChangesAsync();

            // Act
            var extension = await _service.GetFileExtensionAsync(fileId);

            // Assert
            Assert.AreEqual(".png", extension);
        }

        /// <summary>
        /// Тества дали URL адресът за съхранение на файла се обновява успешно.
        /// </summary>
        [Test]
        public async Task UpdateFileUrlAsync_ValidId_UpdatesUrl()
        {
            // Arrange
            var fileId = Guid.NewGuid();
            var file = new FileEntity { Id = fileId, Name = "test", Extension = ".txt", UserId = Guid.NewGuid(), StorageUrl = "old-url", UploadAt = DateTime.UtcNow };
            _dbContext.Files.Add(file);
            await _dbContext.SaveChangesAsync();

            // Act
            await _service.UpdateFileUrlAsync(fileId, "new-cloud-url");

            // Assert
            var updatedFile = await _dbContext.Files.FindAsync(fileId);
            Assert.AreEqual("new-cloud-url", updatedFile.StorageUrl);
        }

        /// <summary>
        /// Тества дали потребителят вижда правилно файловете, които са му изпратени от други.
        /// </summary>
        [Test]
        public async Task GetSharedWithMeFilesAsync_ReturnsFilesSharedWithUser()
        {
            // Arrange
            var receiverId = Guid.NewGuid();
            var sender = new ApplicationUser { Id = Guid.NewGuid(), UserName = "SenderUser", Email = "sender@test.com", FirstName = "Test", LastName = "User" };
            var file = new FileEntity { Id = Guid.NewGuid(), Name = "SharedDoc", Extension = ".pdf", UserId = sender.Id, StorageUrl = "url", UploadAt = DateTime.UtcNow };

            _dbContext.Users.Add(sender);
            _dbContext.Files.Add(file);
            _dbContext.SharedFiles.Add(new SharedFile { FileId = file.Id, SenderId = sender.Id, ReceiverId = receiverId, SharedOn = DateTime.UtcNow });
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _service.GetSharedWithMeFilesAsync(receiverId.ToString());
            var list = result.ToList();

            // Assert
            Assert.AreEqual(1, list.Count);
            Assert.AreEqual("SharedDoc", list[0].Name);
            Assert.AreEqual("SenderUser", list[0].SenderName);
        }

        /// <summary>
        /// Тества дали методът сумира правилно размерите на всички качени файлове.
        /// </summary>
        [Test]
        public async Task GetTotalUsedStorageAsync_ReturnsCorrectSumOfFileSizes()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _dbContext.Files.Add(new FileEntity { Id = Guid.NewGuid(), UserId = userId, Name = "f1", Extension = ".txt", Size = 100, StorageUrl = "url", UploadAt = DateTime.UtcNow });
            _dbContext.Files.Add(new FileEntity { Id = Guid.NewGuid(), UserId = userId, Name = "f2", Extension = ".txt", Size = 250, StorageUrl = "url", UploadAt = DateTime.UtcNow });
            await _dbContext.SaveChangesAsync();

            // Act
            var totalSize = await _service.GetTotalUsedStorageAsync(userId.ToString());

            // Assert
            Assert.AreEqual(350, totalSize);
        }

        /// <summary>
        /// Тества "мекото" изтриване на файл (маркира се като изтрит, но остава в базата).
        /// </summary>
        [Test]
        public async Task DeleteUserFileAsync_SoftDeletesFileSuccessfully()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var file = new FileEntity { Id = Guid.NewGuid(), UserId = userId, Name = "to-delete", Extension = ".txt", StorageUrl = "url", UploadAt = DateTime.UtcNow, IsDeleted = false };
            _dbContext.Files.Add(file);
            await _dbContext.SaveChangesAsync();

            // Act
            await _service.DeleteUserFileAsync(file.Id, userId.ToString());

            // Assert
            var deletedFile = await _dbContext.Files.FindAsync(file.Id);
            Assert.IsTrue(deletedFile.IsDeleted);
            Assert.IsNotNull(deletedFile.DeletedOn);
        }

        /// <summary>
        /// Тества възстановяването на файл от кошчето (премахва флага IsDeleted).
        /// </summary>
        [Test]
        public async Task RestoreUserFileAsync_RestoresSoftDeletedFile()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var file = new FileEntity { Id = Guid.NewGuid(), UserId = userId, Name = "to-restore", Extension = ".txt", StorageUrl = "url", UploadAt = DateTime.UtcNow, IsDeleted = true, DeletedOn = DateTime.UtcNow };
            _dbContext.Files.Add(file);
            await _dbContext.SaveChangesAsync();

            // Act
            await _service.RestoreUserFileAsync(file.Id, userId.ToString());

            // Assert
            var restoredFile = await _dbContext.Files.FindAsync(file.Id);
            Assert.IsFalse(restoredFile.IsDeleted);
            Assert.IsNull(restoredFile.DeletedOn);
        }

        /// <summary>
        /// Тества дали кошчето връща само файловете, които са отбелязани като изтрити.
        /// </summary>
        [Test]
        public async Task GetTrashedFilesAsync_ReturnsOnlyDeletedFilesOrderedByDate()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _dbContext.Files.Add(new FileEntity { Id = Guid.NewGuid(), UserId = userId, Name = "active-file", Extension = ".txt", StorageUrl = "url", UploadAt = DateTime.UtcNow, IsDeleted = false });
            _dbContext.Files.Add(new FileEntity { Id = Guid.NewGuid(), UserId = userId, Name = "trashed-file", Extension = ".pdf", StorageUrl = "url", UploadAt = DateTime.UtcNow, IsDeleted = true, DeletedOn = DateTime.UtcNow });
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _service.GetTrashedFilesAsync(userId.ToString());
            var list = result.ToList();

            // Assert
            Assert.AreEqual(1, list.Count);
            Assert.AreEqual("trashed-file", list[0].Name);
        }

        /// <summary>
        /// Тества физическото изтриване на файл от базата и връщането на неговия URL за изтриване от облака.
        /// </summary>
        [Test]
        public async Task PermanentDeleteFileAsync_RemovesFromDatabaseAndReturnsStorageUrl()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var file = new FileEntity { Id = Guid.NewGuid(), UserId = userId, Name = "perm-delete", Extension = ".txt", StorageUrl = "cloud-url-123", UploadAt = DateTime.UtcNow };
            _dbContext.Files.Add(file);
            await _dbContext.SaveChangesAsync();

            // Act
            var url = await _service.PermanentDeleteFileAsync(file.Id, userId.ToString());

            // Assert
            Assert.AreEqual("cloud-url-123", url);
            Assert.IsFalse(await _dbContext.Files.AnyAsync(f => f.Id == file.Id));
        }

        /// <summary>
        /// Тества масовото изтриване (преместване в кошчето) на няколко файла едновременно.
        /// </summary>
        [Test]
        public async Task DeleteMultipleFilesAsync_SoftDeletesProvidedFiles()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var file1 = new FileEntity { Id = Guid.NewGuid(), UserId = userId, Name = "f1", Extension = ".txt", StorageUrl = "url", UploadAt = DateTime.UtcNow, IsDeleted = false };
            var file2 = new FileEntity { Id = Guid.NewGuid(), UserId = userId, Name = "f2", Extension = ".txt", StorageUrl = "url", UploadAt = DateTime.UtcNow, IsDeleted = false };
            _dbContext.Files.AddRange(file1, file2);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _service.DeleteMultipleFilesAsync(new List<Guid> { file1.Id, file2.Id }, userId.ToString());

            // Assert
            Assert.IsTrue(result);
            Assert.IsTrue((await _dbContext.Files.FindAsync(file1.Id)).IsDeleted);
            Assert.IsTrue((await _dbContext.Files.FindAsync(file2.Id)).IsDeleted);
        }

        /// <summary>
        /// Тества масовото споделяне на няколко файла с даден приятел.
        /// </summary>
        [Test]
        public async Task ShareMultipleFilesAsync_SharesAllProvidedFiles()
        {
            // Arrange
            var senderId = Guid.NewGuid();
            var receiverId = Guid.NewGuid();
            var file1Id = Guid.NewGuid();
            var file2Id = Guid.NewGuid();

            _dbContext.UserFriends.Add(new UserFriend { UserId = senderId, FriendId = receiverId });
            await _dbContext.SaveChangesAsync();

            var ids = new List<Guid> { file1Id, file2Id };

            // Act
            await _service.ShareMultipleFilesAsync(ids, senderId.ToString(), receiverId);

            // Assert
            var sharedRecords = await _dbContext.SharedFiles.ToListAsync();
            Assert.AreEqual(2, sharedRecords.Count);
            Assert.IsTrue(sharedRecords.Any(sf => sf.FileId == file1Id));
            Assert.IsTrue(sharedRecords.Any(sf => sf.FileId == file2Id));
        }

        /// <summary>
        /// Тества пълното изпразване на кошчето и връщането на всички URL-и за триене от облака.
        /// </summary>
        [Test]
        public async Task EmptyTrashAsync_RemovesAllDeletedFilesAndReturnsUrls()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _dbContext.Files.Add(new FileEntity { Id = Guid.NewGuid(), UserId = userId, Name = "f1", Extension = ".txt", StorageUrl = "url1", UploadAt = DateTime.UtcNow, IsDeleted = true });
            _dbContext.Files.Add(new FileEntity { Id = Guid.NewGuid(), UserId = userId, Name = "f2", Extension = ".txt", StorageUrl = "url2", UploadAt = DateTime.UtcNow, IsDeleted = true });
            _dbContext.Files.Add(new FileEntity { Id = Guid.NewGuid(), UserId = userId, Name = "f3", Extension = ".txt", StorageUrl = "url3", UploadAt = DateTime.UtcNow, IsDeleted = false });
            await _dbContext.SaveChangesAsync();

            // Act
            var urls = await _service.EmptyTrashAsync(userId.ToString());

            // Assert
            Assert.IsNotNull(urls);
            Assert.AreEqual(2, urls.Count);
            Assert.Contains("url1", urls);
            Assert.Contains("url2", urls);

            var remainingFiles = await _dbContext.Files.ToListAsync();
            Assert.AreEqual(1, remainingFiles.Count); // Само активният файл трябва да е останал
            Assert.IsFalse(remainingFiles[0].IsDeleted);
        }

        /// <summary>
        /// Тества масовото възстановяване на няколко избрани файла от кошчето.
        /// </summary>
        [Test]
        public async Task RestoreMultipleFilesAsync_RestoresAllProvidedFiles()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var file1 = new FileEntity { Id = Guid.NewGuid(), UserId = userId, Name = "f1", Extension = ".txt", StorageUrl = "url", UploadAt = DateTime.UtcNow, IsDeleted = true, DeletedOn = DateTime.UtcNow };
            var file2 = new FileEntity { Id = Guid.NewGuid(), UserId = userId, Name = "f2", Extension = ".txt", StorageUrl = "url", UploadAt = DateTime.UtcNow, IsDeleted = true, DeletedOn = DateTime.UtcNow };
            _dbContext.Files.AddRange(file1, file2);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _service.RestoreMultipleFilesAsync(new List<Guid> { file1.Id, file2.Id }, userId.ToString());

            // Assert
            Assert.IsTrue(result);
            Assert.IsFalse((await _dbContext.Files.FindAsync(file1.Id)).IsDeleted);
            Assert.IsFalse((await _dbContext.Files.FindAsync(file2.Id)).IsDeleted);
        }

        /// <summary>
        /// Тества дали методът извлича правилните данни (Име, Разширение, URL), нужни за свалянето на файл.
        /// </summary>
        [Test]
        public async Task GetFileToDownloadAsync_ValidFile_ReturnsDownloadViewModel()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var fileId = Guid.NewGuid();
            _dbContext.Files.Add(new FileEntity { Id = fileId, UserId = userId, Name = "my-file", Extension = ".zip", StorageUrl = "cloud-url", UploadAt = DateTime.UtcNow, IsDeleted = false });
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _service.GetFileToDownloadAsync(fileId, userId.ToString());

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("my-file", result.Name);
            Assert.AreEqual(".zip", result.Extension);
            Assert.AreEqual("cloud-url", result.StorageUrl);
        }

        /// <summary>
        /// Тества дали методът връща списък с правилните данни за свалянето на множество файлове.
        /// </summary>
        [Test]
        public async Task GetMultipleFilesToDownloadAsync_ValidFiles_ReturnsListOfDownloadViewModels()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var file1Id = Guid.NewGuid();
            var file2Id = Guid.NewGuid();
            _dbContext.Files.Add(new FileEntity { Id = file1Id, UserId = userId, Name = "f1", Extension = ".txt", StorageUrl = "url1", UploadAt = DateTime.UtcNow, IsDeleted = false });
            _dbContext.Files.Add(new FileEntity { Id = file2Id, UserId = userId, Name = "f2", Extension = ".png", StorageUrl = "url2", UploadAt = DateTime.UtcNow, IsDeleted = false });
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _service.GetMultipleFilesToDownloadAsync(new List<Guid> { file1Id, file2Id }, userId.ToString());

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Any(r => r.Name == "f1" && r.StorageUrl == "url1"));
            Assert.IsTrue(result.Any(r => r.Name == "f2" && r.StorageUrl == "url2"));
        }
    }
}