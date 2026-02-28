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
            // Настройваме in-memory база данни, за да изолираме тестовете.
            // Използваме нов GUID за името, за да сме сигурни, че базата е празна при всяко стартиране.
            var options = new DbContextOptionsBuilder<LuxDriveDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _dbContext = new LuxDriveDbContext(options);

            // Мокваме (фалшифицираме) клиента за DigitalOcean Spaces, 
            // за да не трием реални файлове от облака по време на тестове.
            _mockS3Client = new Mock<IAmazonS3>();

            // Настройваме Dependency Injection контейнера (IServiceProvider), 
            // защото нашият BackgroundService го изисква, за да създаде scope.
            var services = new ServiceCollection();
            services.AddSingleton(_dbContext);

            // Тук използваме малко Reflection, за да вкараме нашия мокнат S3 клиент 
            // в реалния SpacesService, тъй като полето "client" е private.
            var spacesService = new SpacesService();
            var field = typeof(SpacesService).GetField("client", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(spacesService, _mockS3Client.Object);

            services.AddSingleton(spacesService);
            _serviceProvider = services.BuildServiceProvider();
        }

        [TearDown]
        public void TearDown()
        {
            // Изчистваме и освобождаваме ресурсите след всеки тест, 
            // за да не си пречат отделните тестове един на друг.
            _dbContext.Database.EnsureDeleted();
            _dbContext.Dispose();

            if (_serviceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        /// <summary>
        /// Тества дали фоновата задача (BackgroundService) правилно изтрива САМО файлове,
        /// които са в кошчето от повече от 30 дни, като оставя на мира по-скорошно изтритите и активните файлове.
        /// </summary>
        [Test]
        public async Task ExecuteAsync_DeletesOnlyExpiredFiles()
        {
            // Arrange (Подготовка)
            var now = DateTime.UtcNow;
            var userId = Guid.NewGuid();

            // 1. Създаваме стар файл, изтрит преди 31 дни - ТОЙ ТРЯБВА ДА ИЗЧЕЗНЕ.
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

            // 2. Създаваме прясно изтрит файл (преди 5 дни) - ТОЙ ТРЯБВА ДА ОСТАНЕ в кошчето.
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

            // 3. Създаваме активен файл (не е изтрит въобще) - ТОЙ ТРЯБВА ДА ОСТАНЕ непокътнат.
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

            // Добавяме трите файла в тестовата база данни.
            _dbContext.Files.AddRange(oldFile, freshDeletedFile, activeFile);
            await _dbContext.SaveChangesAsync();

            var service = new FileCleanupService(_serviceProvider);

            // Act (Изпълнение)
            // Стартираме фоновата задача с CancellationToken.
            using var cts = new CancellationTokenSource();
            var serviceTask = service.StartAsync(cts.Token);

            // Даваме й 500 милисекунди да се завърти и да свърши работата си.
            await Task.Delay(500);

            // Спираме задачата.
            await service.StopAsync(cts.Token);

            // Assert (Проверка на резултата)
            var remainingFiles = await _dbContext.Files.ToListAsync();

            // Трябва да са останали точно 2 файла (пресният и активният).
            Assert.AreEqual(2, remainingFiles.Count);

            // Проверяваме поименно дали сървисът е свършил точно това, което очакваме.
            Assert.IsFalse(remainingFiles.Any(f => f.Name == "OldFile"), "Старият файл не беше изтрит.");
            Assert.IsTrue(remainingFiles.Any(f => f.Name == "FreshDeleted"), "Файлът от преди 5 дни беше изтрит погрешно.");
            Assert.IsTrue(remainingFiles.Any(f => f.Name == "Active"), "Активният файл беше изтрит погрешно.");
        }
    }
}