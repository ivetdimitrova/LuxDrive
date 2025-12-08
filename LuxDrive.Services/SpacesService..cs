using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;

namespace LuxDrive.Services
{
    public class SpacesService
    {
        private readonly string accessKey = "DO8013YB8VKAUKLVHYVQ";
        private readonly string secretKey = "ECZMfQfsmhpZJfwwZpqWsW274VI+uJuS77pxAvAqoCM";
        private readonly string bucketName = "luxdrive";
        private readonly string region = "ams3";
        private readonly string endpointUrl = "https://luxdrive.ams3.digitaloceanspaces.com";
        private readonly IAmazonS3 client;

        public SpacesService()
        {
            var config = new AmazonS3Config
            {
                ServiceURL = $"https://{region}.digitaloceanspaces.com",
                ForcePathStyle = true
            };

            var credentials = new BasicAWSCredentials(accessKey, secretKey);

            client = new AmazonS3Client(credentials, config);
        }

        public async Task<string> UploadAsync(Stream stream, string key, string contentType)
        {
            var putRequest = new PutObjectRequest
            {
                BucketName = bucketName,
                Key = key,
                InputStream = stream,
                ContentType = contentType,
                CannedACL = S3CannedACL.PublicRead
            };

            await client.PutObjectAsync(putRequest);

            // Публичен URL към файла
            return $"{endpointUrl}/{key}";
        }

        public async Task<List<string>> ListFiles()
        {
            var request = new ListObjectsV2Request
            {
                BucketName = bucketName
            };

            var response = await client.ListObjectsV2Async(request);

            return response.S3Objects.Select(x => x.Key).ToList();
        }
    }
}