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
            private readonly IAmazonS3 client;

            public SpacesService()
            {
                var config = new AmazonS3Config
                {
                    ServiceURL = $"https://{region}.digitaloceanspaces.com",
                    ForcePathStyle = true
                };

                client = new AmazonS3Client(
                    new BasicAWSCredentials(accessKey, secretKey),
                    config
                );
            }

            public async Task<string> UploadFile(Stream fileStream, string fileName)
            {
                var request = new PutObjectRequest
                {
                    BucketName = bucketName,
                    Key = fileName,
                    InputStream = fileStream,
                    CannedACL = S3CannedACL.PublicRead
                };

                await client.PutObjectAsync(request);

                return $"https://{bucketName}.{region}.digitaloceanspaces.com/{fileName}";
            }

            public async Task DeleteFile(string fileName)
            {
                var request = new DeleteObjectRequest
                {
                    BucketName = bucketName,
                    Key = fileName
                };

                await client.DeleteObjectAsync(request);
            }

            public async Task<Stream> DownloadFile(string fileName)
            {
                var request = new GetObjectRequest
                {
                    BucketName = bucketName,
                    Key = fileName
                };

                var response = await client.GetObjectAsync(request);
                return response.ResponseStream;
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