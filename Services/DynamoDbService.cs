using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using DignaApi.Entities;
using DignaApi.Entities.DynamoEntitites;
using DignaApi.Models.Responses;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace DignaApi.Services
{
    public class DynamoDbService  : IDynamoDbService
    {
        private readonly DynamoDBContext _context;
        private readonly ILogger<DynamoDbService> _logger;
        private readonly ICacheService _cacheService;

        private const string UserIdCacheKey = "User_Id_{0}";
        private const string UserEmailCacheKey = "User_Email_{0}";
        private const string AllUsersCacheKey = "All_Users";
        private const string SearchImageByIdCacheKey = "SearchImageById_{0}_Image";
        private const string AllPublicImagesCacheKey = "All_Public_Images";
        private const string PrivateImagesOfUserCacheKey = "PrivateImagesOfUserCacheKey_{0}";
        private const string SearchImagesByTagCacheKey = "SearchImagesByTag _ {0}";
        private const string SearchLikedImagesByUserIdCacheKey = "Search_Liked_Images_UserId_{0}";

        public DynamoDbService(IAmazonDynamoDB dynamoDb, ILogger<DynamoDbService> logger, ICacheService cacheService)
        {
            _context = new DynamoDBContext(dynamoDb);
            _cacheService = cacheService;
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
            string cacheKey = string.Format(UserIdCacheKey,userId);
            var cachedUser = _cacheService.Get<User>(cacheKey);
            if(cachedUser != null)
                return cachedUser;

            var conditions = new List<ScanCondition>
            {
                new ScanCondition("Id", ScanOperator.Equal, userId)
            };

            var results = await _context.ScanAsync<User>(conditions).GetRemainingAsync();
            var user =  results.FirstOrDefault();
            _cacheService.Set<User>(cacheKey, user);
            return user;
        }

        public async Task<User?> GetUserByEmail(string email)
        {
            string cacheKey = string.Format(UserEmailCacheKey, email);
            var cachedUser = _cacheService.Get<User>(cacheKey);
            if (cachedUser != null)
                return cachedUser;

            var queryConfig = new DynamoDBOperationConfig
            {
                IndexName = "Email-index"
            };

            var results = await _context.QueryAsync<User>(
                email,                   // GSI partition key value
                queryConfig
            ).GetRemainingAsync();

            var user =  results.FirstOrDefault();
            _cacheService.Set<User>(cacheKey, user);
            return user;
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            var cachedUser = _cacheService.Get<List<User>>(AllUsersCacheKey);
            if (cachedUser != null)
                return cachedUser;

            var conditions = new List<ScanCondition>();
            var users =  await _context.ScanAsync<User>(conditions).GetRemainingAsync();
            _cacheService.Set<List<User>>(AllUsersCacheKey, users);
            return users;
        }

        public async Task UpdateUserAsync(User user)
        {
            await _context.SaveAsync(user);
            _logger.LogInformation("User updated: {UserId}", user.Id);
            ClearAllUserCaches(user.Id,user.Email);
        }

        public async Task DeleteUserAsync(string userId)
        {
            await _context.DeleteAsync<User>(userId);
            _logger.LogInformation("User deleted: {UserId}", userId);
            ClearAllUserCaches(userId);
        }


        private void ClearAllUserCaches(string userId =null,string email = null)
        {
            //remove all user related cache
            if(!string.IsNullOrEmpty(userId))
            {
                var userIdKey = string.Format(UserIdCacheKey, userId);
                _cacheService.Remove(userIdKey);
            }

            if(!string.IsNullOrEmpty(email))
            {
                var userEmailKey = string.Format(UserEmailCacheKey, email);
                _cacheService.Remove(userEmailKey);
            }
            _cacheService.Remove(AllUsersCacheKey);
        }

        private void ClearAllImageCaches(string userId = null, string imageId = null)
        {
            _cacheService.Remove(AllPublicImagesCacheKey);
            if(!string.IsNullOrEmpty(userId))
            {
                var likedImages = string.Format(SearchLikedImagesByUserIdCacheKey, userId);
                _cacheService.Remove(likedImages);
            }
            if(!string.IsNullOrEmpty(imageId))
            {
                var imageKey = string.Format(SearchImageByIdCacheKey, imageId);
                _cacheService.Remove(imageKey);
            }
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
            var imageKey = string.Format(SearchImagesByTagCacheKey, tag);
            var cachedImages = _cacheService.Get<List<Image>>(imageKey);
            if (cachedImages != null)
                return cachedImages;
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

            var resutlt =  batch.Results;
            _cacheService.Set<List<Image>>(imageKey, resutlt);
            return resutlt;
        }

        public async Task<Image> SearchImageByIdAsync(string id)
        {
            var imageKey = string.Format(SearchImageByIdCacheKey,id);
            var cachedImage = _cacheService.Get<Image>(imageKey);
            if (cachedImage != null)
                return cachedImage;
            var res =  await _context.LoadAsync<Image>(id);
            _cacheService.Set<Image>(imageKey, res);
            return res;
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
            _cacheService.Update<User>(string.Format(UserIdCacheKey,userId), user);
        }

        public async Task LikeImageAsync(string userId, string imageId)
        {
            
            var likedList = await GetImagesLikedByuserAsync(userId);
            Image image = null;
            if(likedList.Any(x => x.Id == imageId))
            {
                image = likedList.FirstOrDefault(x => x.Id == imageId);
                await _context.DeleteAsync<Like>(new Like()
                {
                    ImageId = imageId,
                    UserId = userId,
                    LikedAt = DateTime.UtcNow
                });
                image.Likes -= 1;
            }
            else
            {
                image = await _context.LoadAsync<Image>(imageId);
                await _context.SaveAsync(new Like()
                {
                    ImageId = imageId,
                    UserId = userId,
                    LikedAt = DateTime.UtcNow
                });
                image.Likes += 1;
            }

            await _context.SaveAsync(image);
            _cacheService.Update<Image>(string.Format(SearchImageByIdCacheKey, imageId), image);

            //cclear other image cache
            ClearAllImageCaches(userId);
        }

        public async Task<List<Image>> GetAllImagesForUserAsync(string userid = null)
        {
            List<Image> result = new List<Image>();
            var cahcedPublicImages = _cacheService.Get<List<Image>>(AllPublicImagesCacheKey);
            if(cahcedPublicImages != null && string.IsNullOrEmpty( userid))
            {
                result = cahcedPublicImages;
            }
            else
            {
                var publicConditions = new List<ScanCondition>
                {
                    new ScanCondition("Visibility", ScanOperator.Equal, "public")
                };

                result = await _context.ScanAsync<Image>(publicConditions).GetRemainingAsync();
            }
                

            if (string.IsNullOrEmpty(userid))
            {
                
                return result;

            }
            else
            {
                var privateImages = _cacheService.Get<List<Image>>(string.Format(PrivateImagesOfUserCacheKey, userid));
                if(privateImages != null && privateImages.Count>0)
                {
                    result.AddRange(privateImages);
                }
                else
                {
                    var conditions = new List<ScanCondition>
                    {
                        //new ScanCondition("Visibility", ScanOperator.Equal, "public"),
                        new ScanCondition("UserId", ScanOperator.Equal, userid)
                    };

                    var privateData = await _context.ScanAsync<Image>(conditions).GetRemainingAsync();

                    if (privateData != null && privateData.Count > 0)
                    {
                        var unique = privateData.Where(x => result.Any(y => x.Id != y.Id)).ToList();
                        if (unique != null && unique.Count > 0)
                            result.AddRange(unique);
                    }
                }
                   
                return result;
            }
        }
        public async Task<List<Image>> SearchImagesAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new List<Image>();

            query = query.Trim().ToLowerInvariant();

            var cacheKey = $"Search_Images_Query_{query}";
            var cahcedPublicImages = _cacheService.Get<List<Image>>(cacheKey);
            if(cahcedPublicImages != null)
                return cahcedPublicImages;

            var conditions = new List<ScanCondition>();

            // We'll fetch all items, then filter manually (since DynamoDB doesn't support full text search)
            var allImages = await _context.ScanAsync<Image>(conditions).GetRemainingAsync();

            // Perform client-side filtering (case-insensitive partial match)
           var results = allImages
                .Where(img =>
                    (!string.IsNullOrEmpty(img.Title) && img.Title.ToLower().Contains(query)) ||
                    (!string.IsNullOrEmpty(img.Description) && img.Description.ToLower().Contains(query)) ||
                    (img.Tags != null && img.Tags.Any(tag => tag.ToLower().Contains(query)))
                )
                .OrderByDescending(img => img.CreatedAt)
                .Take(50) // optional: limit results
                .ToList();

            _cacheService.Set<List<Image>>(cacheKey, results);
            return results;
        }

        public async Task<List<Image>> GetImagesByUserid(string userid)
        {
            if (string.IsNullOrWhiteSpace(userid))
                return new List<Image>();

            var cacheKey = $"Search_Images_UserId_{userid}";
            var cahcedPublicImages = _cacheService.Get<List<Image>>(cacheKey);
            if (cahcedPublicImages != null)
                return cahcedPublicImages;

            var conditions = new List<ScanCondition>()
            {
                 new ScanCondition("UserId", ScanOperator.Equal, userid)
            };

            // We'll fetch all items, then filter manually (since DynamoDB doesn't support full text search)
            var allImages = await _context.ScanAsync<Image>(conditions).GetRemainingAsync();

            if(allImages != null && allImages.Count > 0)
              _cacheService.Set<List<Image>>(cacheKey, allImages);
            return allImages;
        }

        public async Task<List<Image>> GetImagesLikedByuserAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return new List<Image>();

            var cacheKey = string.Format(SearchLikedImagesByUserIdCacheKey,userId);
            var cahcedPublicImages = _cacheService.Get<List<Image>>(cacheKey);
            if (cahcedPublicImages != null)
                return cahcedPublicImages;

            var allImages =  await _context.QueryAsync<Like>(
                userId, new DynamoDBOperationConfig
                {
                    IndexName = "UserId-index"
                }
            ).GetRemainingAsync().ContinueWith(async likesTask =>
            {
                var likes = likesTask.Result;
                var batch = _context.CreateBatchGet<Image>();
                foreach (var like in likes)
                {
                    batch.AddKey(like.ImageId);
                }
                await batch.ExecuteAsync();
                return batch.Results;
            }).Unwrap();

            if (allImages != null && allImages.Count > 0)
                _cacheService.Set<List<Image>>(cacheKey, allImages);
            return allImages;
        }
    }
}
