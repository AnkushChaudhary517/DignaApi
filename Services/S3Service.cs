using Amazon.S3;
using Amazon.S3.Transfer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;

namespace DignaApi.Services
{
    public class S3Service : IS3Service
    {
        private readonly IAmazonS3 _s3Client;
        private readonly IConfiguration _config;
        private readonly ILogger<S3Service> _logger;
        private readonly HttpClient _httpClient;

        public S3Service(
            IAmazonS3 s3Client,
            IConfiguration config,
            ILogger<S3Service> logger)
        {
            _s3Client = s3Client;
            _config = config;
            _logger = logger;
            _httpClient = new HttpClient();
        }

        // ✅ Upload to Amazon S3
        public async Task<string> UploadToS3Async(Stream fileStream, string fileName, string contentType)
        {
            var bucketName = _config["AWS:BucketName"];

            var uploadRequest = new TransferUtilityUploadRequest
            {
                InputStream = fileStream,
                Key = fileName,
                BucketName = bucketName,
                ContentType = contentType,
                //CannedACL = S3CannedACL.PublicRead // make public if required
            };

            var fileTransferUtility = new TransferUtility(_s3Client);

            try
            {
                await fileTransferUtility.UploadAsync(uploadRequest);
                string fileUrl = $"https://{bucketName}.s3.amazonaws.com/{fileName}";
                _logger.LogInformation("File uploaded to S3: {url}", fileUrl);
                return fileUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file to S3");
                throw;
            }
        }

        // ✅ Upload to CDN (HTTP-based endpoint)
        public async Task<string> UploadToCdnAsync(Stream fileStream, string fileName, string contentType)
        {
            var cdnUrl = _config["CDN:UploadEndpoint"]; // e.g., https://cdn.example.com/upload

            using var content = new MultipartFormDataContent();
            var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
            content.Add(fileContent, "file", fileName);

            try
            {
                var response = await _httpClient.PostAsync(cdnUrl, content);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("File uploaded to CDN: {result}", result);
                return result; // Typically returns uploaded URL
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file to CDN");
                throw;
            }
        }
    }
}
