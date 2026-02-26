using NUnit.Framework;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using LuxDrive.Data;
using LuxDrive.Data.Models;
using LuxDrive.Services;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.S3;

namespace LuxDrive.Tests.Services
{
    [TestFixture]
    public class FileCleanupServiceTests
    {
        private LuxDriveDbContext _dbContext;
        private IServiceProvider _serviceProvider;
        private Mock<IAmazonS3> _mockS3Client;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<LuxDriveDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _dbContext = new LuxDriveDbContext(options);

            _mockS3Client = new Mock<IAmazonS3>();

            var services = new ServiceCollection();
            services.AddSingleton(_dbContext);

            var spacesService = new SpacesService();
            var field = typeof(SpacesService).GetField("client", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(spacesService, _mockS3Client.Object);

            services.AddSingleton(spacesService);
            _serviceProvider = services.BuildServiceProvider();
        }

        [TearDown]
        public void TearDown()
        {
            _dbContext.Database.EnsureDeleted();
            _dbContext.Dispose();

            if (_serviceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
        [Test]
        public async Task ExecuteAsync_DeletesOnlyExpiredFiles()
        {
            // Arrange 
            var now = DateTime.UtcNow;
            var userId = Guid.NewGuid();

            var oldFile = new LuxDrive.Data.Models.File
            {
                Id = Guid.NewGuid(),
                Name = "OldFile",
                Extension = ".png",
                StorageUrl = "https://luxdrive.ams3.digitaloceanspaces.com/old.png",
                IsDeleted = true,
                DeletedOn = now.AddDays(-31),
                UserId = userId,
                UploadAt = now.AddDays(-40)
            };

            var freshDeletedFile = new LuxDrive.Data.Models.File
            {
                Id = Guid.NewGuid(),
                Name = "FreshDeleted",
                Extension = ".jpg",
                StorageUrl = "https://luxdrive.ams3.digitaloceanspaces.com/fresh.jpg",
                IsDeleted = true,
                DeletedOn = now.AddDays(-5),
                UserId = userId,
                UploadAt = now.AddDays(-10)
            };

            var activeFile = new LuxDrive.Data.Models.File
            {
                Id = Guid.NewGuid(),
                Name = "Active",
                Extension = ".pdf",
                StorageUrl = "https://luxdrive.ams3.digitaloceanspaces.com/active.pdf",
                IsDeleted = false,
                UserId = userId,
                UploadAt = now
            };

            _dbContext.Files.AddRange(oldFile, freshDeletedFile, activeFile);
            await _dbContext.SaveChangesAsync();

            var service = new FileCleanupService(_serviceProvider);

            // Act 
            using var cts = new CancellationTokenSource();
            var serviceTask = service.StartAsync(cts.Token);

            await Task.Delay(500); 

            await service.StopAsync(cts.Token);

            // Assert 
            var remainingFiles = await _dbContext.Files.ToListAsync();

            Assert.AreEqual(2, remainingFiles.Count);
            Assert.IsFalse(remainingFiles.Any(f => f.Name == "OldFile"), "Старият файл не беше изтрит.");
            Assert.IsTrue(remainingFiles.Any(f => f.Name == "FreshDeleted"), "Файлът от преди 5 дни беше изтрит погрешно.");
            Assert.IsTrue(remainingFiles.Any(f => f.Name == "Active"), "Активният файл беше изтрит погрешно.");
        }
    }
}