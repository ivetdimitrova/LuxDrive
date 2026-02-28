using NUnit.Framework;
using Moq;
using Amazon.S3;
using Amazon.S3.Model;
using LuxDrive.Services;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace LuxDrive.Tests.Services
{
    [TestFixture]
    public class SpacesServiceTests
    {
        private Mock<IAmazonS3> _mockS3Client;
        private SpacesService _service;

        [SetUp]
        public void Setup()
        {
            // Създаваме "мокнат" (фалшив) клиент за Amazon S3, 
            // за да можем да тестваме логиката, без реално да пращаме заявки към DigitalOcean.
            _mockS3Client = new Mock<IAmazonS3>();
            _service = new SpacesService();

            // Тъй като полето "client" в SpacesService е private, 
            // използваме Reflection, за да му присвоим нашия фалшив S3 клиент.
            var field = typeof(SpacesService).GetField("client", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(_service, _mockS3Client.Object);
        }

        /// <summary>
        /// Тества дали методът UploadAsync изпраща правилна заявка към облака 
        /// и дали връща правилно конструиран URL адрес за достъп до файла.
        /// </summary>
        [Test]
        public async Task UploadAsync_ReturnsCorrectUrl()
        {
            // Arrange (Подготовка)
            var stream = new MemoryStream();
            var key = "test.png";

            // Настройваме фалшивия клиент винаги да връща успешен отговор, 
            // когато се извика PutObjectAsync (методът за качване в AWS SDK).
            _mockS3Client.Setup(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
                         .ReturnsAsync(new PutObjectResponse());

            // Act (Изпълнение)
            var url = await _service.UploadAsync(stream, key, "image/png");

            // Assert (Проверка)
            // Уверяваме се, че върнатият линк съдържа името на файла и точния домейн на нашия сървър.
            StringAssert.Contains(key, url);
            Assert.AreEqual("https://luxdrive.ams3.digitaloceanspaces.com/test.png", url);
        }

        /// <summary>
        /// Тества дали методът DeleteAsync наистина извиква функционалността за изтриване 
        /// на AWS SDK с правилния идентификатор (key) на файла.
        /// </summary>
        [Test]
        public async Task DeleteAsync_CallsDeleteObject()
        {
            // Arrange (Подготовка)
            var key = "file-to-delete.png";

            // Настройваме мока да симулира успешно изтриване.
            _mockS3Client.Setup(x => x.DeleteObjectAsync(It.IsAny<DeleteObjectRequest>(), It.IsAny<CancellationToken>()))
                         .ReturnsAsync(new DeleteObjectResponse());

            // Act (Изпълнение)
            await _service.DeleteAsync(key);

            // Assert (Проверка)
            // Проверяваме дали методът DeleteObjectAsync е бил извикан ТОЧНО един път (Times.Once)
            // и дали му е подаден правилният ключ (Key == key).
            _mockS3Client.Verify(x => x.DeleteObjectAsync(It.Is<DeleteObjectRequest>(r => r.Key == key), It.IsAny<CancellationToken>()), Times.Once);
        }

        /// <summary>
        /// Тества дали методът ListFiles успешно взима и обработва списъка с обекти от облака, 
        /// връщайки само техните ключове (имена).
        /// </summary>
        [Test]
        public async Task ListFiles_ReturnsListOfKeys()
        {
            // Arrange (Подготовка)
            // Създаваме примерен отговор, който сървърът би върнал - списък с 1 файл.
            var response = new ListObjectsV2Response
            {
                S3Objects = new List<S3Object>
                {
                    new S3Object { Key = "file1.jpg" }
                }
            };

            // Настройваме мока да връща този конкретен отговор при повикване на ListObjectsV2Async.
            _mockS3Client.Setup(x => x.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), It.IsAny<CancellationToken>()))
                         .ReturnsAsync(response);

            // Act (Изпълнение)
            var result = await _service.ListFiles();

            // Assert (Проверка)
            // Проверяваме дали резултатът съдържа името на файла от нашия "фалшив" отговор.
            Assert.Contains("file1.jpg", result.ToList());
        }
    }
}