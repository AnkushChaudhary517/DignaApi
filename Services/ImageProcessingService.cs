namespace DignaApi.Services
{
    using Amazon.DynamoDBv2;
    using Amazon.DynamoDBv2.Model;
    using Amazon.S3;
    using Amazon.S3.Transfer;
    using DignaApi.Entities;
    using DignaApi.Models.Requests;
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
        private readonly IAmazonDynamoDB _dynamoDb;

        public ImageProcessingService(IAmazonS3 s3Client, IConfiguration config, IDynamoDbService dynamoDbService,
            IAmazonDynamoDB dynamoDb)
        {
            _s3Client = s3Client;
            _config = config;
            _bucketName = _config["AWS:BucketName"];
            _dynamoDbService = dynamoDbService;
            _dynamoDb = dynamoDb;
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
                    ContentType = "image/jpeg",
                    //ContentDisposition = "inline",
                    CannedACL = S3CannedACL.PublicRead
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

        public Task<List<Entities.DynamoEntitites.Image>> SearchImagesAsync(string q)
        {
            return _dynamoDbService.SearchImagesAsync(q);
        }

        public Task<List<Entities.DynamoEntitites.Image>> GetImagesByUserid(string userId)
        {
            return _dynamoDbService.GetImagesByUserid(userId);
        }
        public Task<List<Entities.DynamoEntitites.Image>> GetImagesLikedByuserAsync(string userId)
        {
            return _dynamoDbService.GetImagesLikedByuserAsync(userId);
        }

        public async Task<DownloadEvent> TrackDownloadAsync(string imageId, string? userId, TrackDownloadRequest request, string? userAgent = null, string? referer = null)
        {
            if (string.IsNullOrEmpty(imageId))
                throw new ArgumentException("imageId is required", nameof(imageId));

            await EnsureTableExistsAsync();

            var ev = new DownloadEvent
            {
                ImageId = imageId,
                UserId = userId,
                Title = request.Title ?? string.Empty,
                ImageUrl = request.ImageUrl ?? string.Empty,
                Photographer = request.Photographer ?? string.Empty,
                SizeId = request.SizeId ?? string.Empty,
                CreatedAt = DateTime.UtcNow
            };

            var item = new Dictionary<string, AttributeValue>
            {
                ["Id"] = new AttributeValue { S = ev.Id },
                ["ImageId"] = new AttributeValue { S = ev.ImageId },
                ["UserId"] = ev.UserId != null ? new AttributeValue { S = ev.UserId } : new AttributeValue { NULL = true },
                ["Title"] = new AttributeValue { S = ev.Title },
                ["ImageUrl"] = new AttributeValue { S = ev.ImageUrl },
                ["Photographer"] = new AttributeValue { S = ev.Photographer },
                ["SizeId"] = new AttributeValue { S = ev.SizeId },
                ["CreatedAt"] = new AttributeValue { S = ev.CreatedAt.ToString("o") }
            };

            // Optional metadata
            if (!string.IsNullOrEmpty(userAgent))
                item["UserAgent"] = new AttributeValue { S = userAgent };

            if (!string.IsNullOrEmpty(referer))
                item["Referer"] = new AttributeValue { S = referer };

            await _dynamoDb.PutItemAsync(new PutItemRequest
            {
                TableName = "Downloads",
                Item = item
            });

            return ev;
        }
        private async Task EnsureTableExistsAsync()
        {
            try
            {
                // Check if table exists
                await _dynamoDb.DescribeTableAsync(new DescribeTableRequest { TableName = "Downloads" });
                return;
            }
            catch (ResourceNotFoundException)
            {
                // create table
                var request = new CreateTableRequest
                {
                    TableName = "Downloads",
                    AttributeDefinitions = new List<AttributeDefinition>
                {
                    new AttributeDefinition { AttributeName = "Id", AttributeType = "S" },
                    new AttributeDefinition { AttributeName = "ImageId", AttributeType = "S" } // for potential queries
                },
                    KeySchema = new List<KeySchemaElement>
                {
                    new KeySchemaElement { AttributeName = "Id", KeyType = "HASH" }
                },
                    BillingMode = BillingMode.PAY_PER_REQUEST
                };

                // create a GSI on ImageId to query events by image if needed later
                request.GlobalSecondaryIndexes = new List<GlobalSecondaryIndex>
            {
                new GlobalSecondaryIndex
                {
                    IndexName = "ImageIdIndex",
                    KeySchema = new List<KeySchemaElement> { new KeySchemaElement { AttributeName = "ImageId", KeyType = "HASH" } },
                    Projection = new Projection { ProjectionType = "ALL" }
                }
            };

                await _dynamoDb.CreateTableAsync(request);

                // Wait until active (simple loop)
                for (int i = 0; i < 20; i++)
                {
                    var desc = await _dynamoDb.DescribeTableAsync(new DescribeTableRequest { TableName = "Downloads" });
                    if (desc.Table.TableStatus == TableStatus.ACTIVE)
                        return;
                    await Task.Delay(1000);
                }
            }
        }
        public async Task<List<DownloadEvent>> GetDownloadsByImageIdAsync(string imageId, int limit = 50)
        {
            var results = new List<DownloadEvent>();
            if (string.IsNullOrEmpty(imageId))
                return results;

            try
            {
                // Query the GSI ImageIdIndex
                var request = new QueryRequest
                {
                    TableName = "Downloads",
                    IndexName = "ImageIdIndex",
                    KeyConditionExpression = "ImageId = :iid",
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    { ":iid", new AttributeValue { S = imageId } }
                },
                    Limit = limit,
                    ScanIndexForward = false // latest first
                };

                var response = await _dynamoDb.QueryAsync(request);
                if (response.Items != null)
                {
                    foreach (var item in response.Items)
                    {
                        results.Add(MapItemToDownloadEvent(item));
                    }
                }
            }
            catch
            {
                // swallow and return what we have
            }

            return results;
        }

        public async Task<int> GetDownloadCountByImageIdAsync(string imageId)
        {
            if (string.IsNullOrEmpty(imageId))
                return 0;

            try
            {
                var request = new QueryRequest
                {
                    TableName = "Downloads",
                    IndexName = "ImageIdIndex",
                    KeyConditionExpression = "ImageId = :iid",
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    { ":iid", new AttributeValue { S = imageId } }
                },
                    Select = Select.COUNT
                };

                var response = await _dynamoDb.QueryAsync(request);
                return response.Count.Value;
            }
            catch
            {
                return 0;
            }
        }

        private DownloadEvent MapItemToDownloadEvent(Dictionary<string, AttributeValue> item)
        {
            try
            {
                var id = item.TryGetValue("Id", out var idAttr) ? idAttr.S : Guid.NewGuid().ToString();
                var ImageId = item.TryGetValue("ImageId", out var ii) ? ii.S : string.Empty;
                var UserId = item.TryGetValue("UserId", out var ui)? ui.S : null;
                var Title = item.TryGetValue("Title", out var t) ? t.S : string.Empty;
                var ImageUrl = item.TryGetValue("ImageUrl", out var iu) ? iu.S : string.Empty;
                var Photographer = item.TryGetValue("Photographer", out var p) ? p.S : string.Empty;
                var SizeId = item.TryGetValue("SizeId", out var s) ? s.S : string.Empty;
                var ev = new DownloadEvent
                {
                    Id = id,
                    ImageId = ImageId,
                    UserId = UserId,
                    Title = Title,
                    ImageUrl = ImageUrl,
                    Photographer = Photographer,
                    SizeId = SizeId,
                    //UserAgent = item.TryGetValue("UserAgent", out var ua) ? ua.S : null,
                    //Referer = item.TryGetValue("Referer", out var r) ? r.S : null
                };

                if (item.TryGetValue("CreatedAt", out var ca) && !string.IsNullOrEmpty(ca.S))
                {
                    if (DateTime.TryParse(ca.S, out var dt))
                        ev.CreatedAt = dt;
                    else
                        ev.CreatedAt = DateTime.UtcNow;
                }
                else
                {
                    ev.CreatedAt = DateTime.UtcNow;
                }

                return ev;
            }
            catch
            {

            }
            return null;
        }
        public async Task<int> GetDownloadCountByUserIdAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return 0;

            try
            {
                int total = 0;
                Dictionary<string, AttributeValue>? lastKey = null;

                do
                {
                    var scanRequest = new ScanRequest
                    {
                        TableName = "Downloads",
                        Select = Select.COUNT,
                        FilterExpression = "UserId = :uid",
                        ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                    {
                        { ":uid", new AttributeValue { S = userId } }
                    },
                        ExclusiveStartKey = lastKey
                    };

                    var response = await _dynamoDb.ScanAsync(scanRequest);
                    total += response.Count.Value;
                    lastKey = response.LastEvaluatedKey;
                } while (lastKey != null && lastKey.Count > 0);

                return total;
            }
            catch
            {
                return 0;
            }
        }

        public async Task<List<DownloadEvent>> GetDownloadsByUserIdAsync(string userId, int limit = 50)
        {
            var result = new List<DownloadEvent>();
            if (string.IsNullOrEmpty(userId)) return result;

            // Try query on a UserId GSI first (recommended). If the index doesn't exist or query fails, fall back to a scan.
            try
            {
                var queryRequest = new QueryRequest
                {
                    TableName = "Downloads",
                    IndexName = "UserIdIndex",
                    KeyConditionExpression = "UserId = :uid",
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                    {
                        { ":uid", new AttributeValue { S = userId } }
                    },
                    Limit = limit,
                    ScanIndexForward = false // newest first
                };

                var queryResponse = await _dynamoDb.QueryAsync(queryRequest);
                if (queryResponse.Items != null)
                {
                    foreach (var item in queryResponse.Items)
                    {
                        result.Add(MapItemToDownloadEvent(item));
                        if (result.Count >= limit) break;
                    }
                }

                return result;
            }
            catch (ResourceNotFoundException)
            {
                // index doesn't exist -> fall through to scan
            }
            catch
            {
                // query failed for some reason -> fall through to scan
            }

            // Fallback: scan with filter (inefficient on large tables)
            try
            {
                Dictionary<string, AttributeValue>? lastKey = null;
                while (result.Count < limit)
                {
                    var scanRequest = new ScanRequest
                    {
                        TableName = "Downloads",
                        FilterExpression = "UserId = :uid",
                        ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                        {
                            { ":uid", new AttributeValue { S = userId } }
                        },
                        Limit = Math.Min(limit - result.Count, 100),
                        ExclusiveStartKey = lastKey
                    };

                    var scanResponse = await _dynamoDb.ScanAsync(scanRequest);
                    if (scanResponse.Items != null)
                    {
                        foreach (var item in scanResponse.Items)
                        {
                            result.Add(MapItemToDownloadEvent(item));
                            if (result.Count >= limit) break;
                        }
                    }

                    lastKey = scanResponse.LastEvaluatedKey;
                    if (lastKey == null || lastKey.Count == 0) break;
                }
            }
            catch
            {
                // swallow and return what we have
            }

            return result;
        }

    }

}
