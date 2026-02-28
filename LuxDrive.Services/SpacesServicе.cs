using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using LuxDrive.Services.Interfaces;

namespace LuxDrive.Services
{
    public class SpacesService : ISpacesService
    {
        //Ключове за сигурност 

        /// <summary>
        /// Публичен идентификатор на облачния сървър
        /// </summary>
        private readonly string accessKey = "DO8013YB8VKAUKLVHYVQ";

        /// <summary>
        /// Таен ключ, който подписва заявките към сървъра
        /// </summary>
        private readonly string secretKey = "ECZMfQfsmhpZJfwwZpqWsW274VI+uJuS77pxAvAqoCM";

        /// <summary>
        /// Име на контейнера , в който се пазят данните
        /// </summary>
        private readonly string bucketName = "luxdrive";

        /// <summary>
        /// Къде се намира сървъра
        /// </summary>
        private readonly string region = "ams3";

        /// <summary>
        /// Основен адрес за връзка с сървъра - DigitalOcean Spaces.
        /// </summary>
        private readonly string endpointUrl = "https://luxdrive.ams3.digitaloceanspaces.com";

        /// <summary>
        /// Клиент за изпълнение на операции към S3 съвместимото хранилище.
        /// </summary>
        private readonly IAmazonS3 client;


        /// <summary>
        /// Конструктор на услугата, който инициира връзката с DigitalOcean Spaces.
        /// Тук се задават конфигурациите за региона, подават се тайните ключове за достъп (credentials) 
        /// и се създава S3 клиентът, чрез който се извършват всички операции с файлове.
        /// </summary>
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

        /// <summary>
        /// Метод за качване на файл в облачното хранилище.
        /// Създава заявка към облачния сървър(DigitalOcean), която включва самия файл (stream), уникалното му име (key) и неговия тип. 
        /// Настройва файла да бъде публично достъпен за четене и накрая връща пълния му интернет адрес (URL).
        /// </summary>
        /// <param name="stream">Потокът от данни на файла.</param>
        /// <param name="key">Уникалното име, под което файлът ще се запише в облака.</param>
        /// <param name="contentType">Типът на файла (напр. image/jpeg, application/pdf).</param>
        /// <returns>Връща готовия линк към качените данни.</returns>
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

            return $"{endpointUrl}/{key}";
        }

        /// <summary>
        /// Метод за извличане на списък с имената (ключовете) на всички файлове в облачното хранилище.
        /// Прави заявка към контейнера (Bucket) и връща само уникалните идентификатори на обектите, които се съхраняват там.
        /// </summary>
        /// <returns>Списък от текстови низове, представляващи имената на файловете в облака.</returns>
        public async Task<List<string>> ListFiles()
        {
            var request = new ListObjectsV2Request
            {
                BucketName = bucketName
            };

            var response = await client.ListObjectsV2Async(request);

            return response.S3Objects.Select(x => x.Key).ToList();
        }


        /// <summary>
        /// Метод за окончателно изтриване на файл от облачното хранилище.
        /// Изпраща заявка към конкретния контейнер (Bucket), като използва уникалния ключ на файла, за да го премахне физически от сървъра.
        /// </summary>
        /// <param name="key">Уникалното име (пътят) на файла, който трябва да бъде изтрит.</param>
        public async Task DeleteAsync(string key)
        {
            var deleteRequest = new DeleteObjectRequest
            {
                BucketName = bucketName,
                Key = key
            };

            await client.DeleteObjectAsync(deleteRequest);
        }

    }
}