namespace DignaApi.Services
{
    using Amazon.S3;
    using Amazon.S3.Transfer;
    using DignaApi.Models.Responses;
    using SixLabors.ImageSharp;
    using SixLabors.ImageSharp.Processing;
    using System.Linq;

    public class ImageProcessingService : IImageProcessingService
    {
        private readonly IAmazonS3 _s3Client;
        private readonly string _bucketName;
        //private readonly string _cdnBaseUrl = "https://your-cloudfront-url-or-s3-url";
        private readonly IConfiguration _config;
        private readonly IDynamoDbService _dynamoDbService;

        public ImageProcessingService(IAmazonS3 s3Client, IConfiguration config, IDynamoDbService dynamoDbService)
        {
            _s3Client = s3Client;
            _config = config;
            _bucketName = _config["AWS:BucketName"];
            _dynamoDbService = dynamoDbService;
        }

        public Task<List<Entities.DynamoEntitites.Image>> GetImagesFromTagAsync(string tag)
        {
            return _dynamoDbService.SearchImagesByTagAsync(tag);
        }

        public async Task<Dictionary<string, string>> UploadImageWithQualitiesAsync(Stream originalImageStream,DignaApi.Entities.DynamoEntitites.Image img)
        {
            var resultUrls = new Dictionary<string, string>();
            var folderPath = $"users/{img.UserId}/images/{Guid.NewGuid()}";

            // Define output resolutions
            var qualities = new Dictionary<string, int>
        {
            { "low", 480 },
            { "medium", 1080 },
            { "high", 1920 }
        };

            // Load image once
            using var image = await Image.LoadAsync(originalImageStream);



            foreach (var kvp in qualities)
            {
                var label = kvp.Key;
                var width = kvp.Value;

                // Resize proportionally
                using var clone = image.Clone(ctx =>
                {
                    ctx.Resize(new ResizeOptions
                    {
                        Size = new Size(width, 0), // maintain aspect ratio
                        Mode = ResizeMode.Max
                    });
                });

                // Save to memory stream
                using var ms = new MemoryStream();
                await clone.SaveAsJpegAsync(ms);
                ms.Position = 0;

                // Upload to S3
                var s3Key = $"{folderPath}/{label}_{img.Title}";
                var fileTransferUtility = new TransferUtility(_s3Client);
                await fileTransferUtility.UploadAsync(ms, _bucketName, s3Key);
                var uploadRequest = new TransferUtilityUploadRequest
                {
                    InputStream = ms,
                    Key = img.Title,
                    BucketName = _bucketName,
                    //ContentType = contentType,
                    //CannedACL = S3CannedACL.PublicRead // make public if required
                };
                string fileUrl = $"https://{_bucketName}.s3.amazonaws.com/{s3Key}";
                // Add to result map
                resultUrls[label] = fileUrl;
                if(label == "high")
                {
                    img.ImageUrl = fileUrl;
                    img.AspectRatio = ((float)image.Width / image.Height).ToString();
                }
            }
            img.QualityUrls = resultUrls;

            _dynamoDbService.SaveTagsAsync(img);
            return resultUrls;
        }

        public Task<Entities.DynamoEntitites.Image> GetImageFromId(string imageId)
        {
            return  _dynamoDbService.SearchImageByIdAsync(imageId);
        }

        public Task LikeImageAsync(string userId, string id)
        {
            return _dynamoDbService.LikeImageAsync(userId, id);
        }

        public Task<List<DignaApi.Entities.DynamoEntitites.Image>> GetAllImagesAsync(string userid = null)
        {
            return  _dynamoDbService.GetAllImagesForUserAsync(userid);
            
        }
    }

}
