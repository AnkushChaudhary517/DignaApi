using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using DignaApi.Entities;
using DignaApi.Entities.DynamoEntitites;
using DignaApi.Models.Responses;

namespace DignaApi.Services
{
    public class DynamoDbService  : IDynamoDbService
    {
        private readonly DynamoDBContext _context;
        private readonly ILogger<DynamoDbService> _logger;

        public DynamoDbService(IAmazonDynamoDB dynamoDb, ILogger<DynamoDbService> logger)
        {
            _context = new DynamoDBContext(dynamoDb);
            _logger = logger;
        }

        public async Task CreateUserAsync(User user)
        {
            user.Id = Guid.NewGuid().ToString();
            await _context.SaveAsync(user);
            _logger.LogInformation("User created: {UserId}", user.Id);
        }

        public async Task<User?> GetUserAsync(string userId)
        {
            return await _context.LoadAsync<User>(userId);
        }

        public async Task<User?> GetUserByEmail(string email)
        {
            var queryConfig = new DynamoDBOperationConfig
            {
                IndexName = "Email-index"
            };

            var results = await _context.QueryAsync<User>(
                email,                   // GSI partition key value
                queryConfig
            ).GetRemainingAsync();

            return results.FirstOrDefault();
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            var conditions = new List<ScanCondition>();
            return await _context.ScanAsync<User>(conditions).GetRemainingAsync();
        }

        public async Task UpdateUserAsync(User user)
        {
            await _context.SaveAsync(user);
            _logger.LogInformation("User updated: {UserId}", user.Id);
        }

        public async Task DeleteUserAsync(string userId)
        {
            await _context.DeleteAsync<User>(userId);
            _logger.LogInformation("User deleted: {UserId}", userId);
        }
        public async Task SaveTagsAsync(Image image)
        {
            await _context.SaveAsync(image);
            if(image.Tags == null || !image.Tags.Any())
                return;
            // 2️⃣ Create tag index entries
            foreach (var tag in image.Tags)
            {
                var tagItem = new TagIndex
                {
                    Tag = tag.ToLower(),
                    ImageId = image.Id,
                    UserId = image.UserId,
                    Visibility = image.Visibility,
                    CreatedAt = image.CreatedAt
                };

                await _context.SaveAsync(tagItem);
            }
        }
        public async Task<List<Image>> SearchImagesByTagAsync(string tag)
        {
            // 1️⃣ Query the TagIndex
            var tagItems = await _context.QueryAsync<TagIndex>(
                tag.ToLower(), new DynamoDBOperationConfig
                {
                    IndexName = "Tag-index"
                }
            ).GetRemainingAsync();

            // 2️⃣ Get public ImageIds only
            var imageIds = tagItems
                .Where(t => t.Visibility == "public")
                .Select(t => t.ImageId)
                .ToList();

            // 3️⃣ Batch get images from main table
            var batch = _context.CreateBatchGet<Image>();
            foreach (var id in imageIds)
                batch.AddKey(id);
            await batch.ExecuteAsync();

            return batch.Results;
        }

        public async Task<Image> SearchImageByIdAsync(string id)
        {

            return await _context.LoadAsync<Image>(id);
        }

        public async Task FollowUserAsync(string userId, string followeeId)
        {
            var user = await _context.LoadAsync<User>(userId);
             await _context.SaveAsync(new Follow
            {
                FollowerId = userId,
                FolloweeId = followeeId,
                FollowedAt = DateTime.UtcNow
            });
            user.Followers += 1;
            await _context.SaveAsync(user);
        }

        public async Task LikeImageAsync(string userId, string imageId)
        {
            var image = await _context.LoadAsync<Image> (userId, imageId);
             await _context.SaveAsync(new Like()
            {
                ImageId = imageId,
                UserId = userId,
                LikedAt = DateTime.UtcNow
            });
            image.Likes += 1;
            await _context.SaveAsync(image);
        }

        public async Task<List<Image>> GetAllImagesForUserAsync(string userid = null)
        {

            var publicConditions = new List<ScanCondition>
                {
                    new ScanCondition("Visibility", ScanOperator.Equal, "public")
                };

            var result = await _context.ScanAsync<Image>(publicConditions).GetRemainingAsync();

            if (string.IsNullOrEmpty(userid))
            {
                
                return result;

            }
            else
            {
                var conditions = new List<ScanCondition>
                {
                    //new ScanCondition("Visibility", ScanOperator.Equal, "public"),
                    new ScanCondition("userId", ScanOperator.Equal, userid)
                };

                var privateData = await _context.ScanAsync<Image>(conditions).GetRemainingAsync();

                if(privateData != null && privateData.Count>0)
                {
                    var unique = privateData.Where(x => result.Any(y => x.Id != y.Id)).ToList();
                    if (unique != null && unique.Count > 0)
                        result.AddRange(unique);
                }
                return result;
            }
        }
    }
}
