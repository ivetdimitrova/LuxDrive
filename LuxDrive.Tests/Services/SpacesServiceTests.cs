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
            _mockS3Client = new Mock<IAmazonS3>();
            _service = new SpacesService();
            var field = typeof(SpacesService).GetField("client", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(_service, _mockS3Client.Object);
        }

        [Test]
        public async Task UploadAsync_ReturnsCorrectUrl()
        {
            // Arrange
            var stream = new MemoryStream();
            var key = "test.png";
            _mockS3Client.Setup(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
                         .ReturnsAsync(new PutObjectResponse());

            // Act
            var url = await _service.UploadAsync(stream, key, "image/png");

            // Assert
            StringAssert.Contains(key, url);
            Assert.AreEqual("https://luxdrive.ams3.digitaloceanspaces.com/test.png", url);
        }

        [Test]
        public async Task DeleteAsync_CallsDeleteObject()
        {
            // Arrange
            var key = "file-to-delete.png";
            _mockS3Client.Setup(x => x.DeleteObjectAsync(It.IsAny<DeleteObjectRequest>(), It.IsAny<CancellationToken>()))
                         .ReturnsAsync(new DeleteObjectResponse());

            // Act
            await _service.DeleteAsync(key);

            // Assert
            _mockS3Client.Verify(x => x.DeleteObjectAsync(It.Is<DeleteObjectRequest>(r => r.Key == key), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task ListFiles_ReturnsListOfKeys()
        {
            // Arrange
            var response = new ListObjectsV2Response
            {
                S3Objects = new List<S3Object>
        {
            new S3Object { Key = "file1.jpg" }
        }
            };
            _mockS3Client.Setup(x => x.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), It.IsAny<CancellationToken>()))
                         .ReturnsAsync(response);

            // Act
            var result = await _service.ListFiles();

            // Assert
            Assert.Contains("file1.jpg", result.ToList());
        }
    }
}