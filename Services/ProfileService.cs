using System.Text;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.EntityFrameworkCore;
using DignaApi.Data;
using DignaApi.Models.Requests;
using DignaApi.Models.Responses;
using DignaApi.Models;

namespace DignaApi.Services;

public class ProfileService : IProfileService
{
    private readonly DignaDbContext _context;
    private readonly IS3Service _s3Service;
    private readonly IDynamoDbService _dynamoDbService;
    private readonly IAmazonDynamoDB _dynamoDbClient;
    private const string FollowTableName = "Follow";

    public ProfileService(DignaDbContext context, IS3Service s3Service, IDynamoDbService dynamoDbService, IAmazonDynamoDB dynamoDbClient)
    {
        _context = context;
        _s3Service = s3Service;
        _dynamoDbService = dynamoDbService;
        _dynamoDbClient = dynamoDbClient;
    }

    public async Task<bool> FollowerExistsAsync(string followeeId)
    {
        if (string.IsNullOrEmpty(followeeId))
            return false;

        try
        {
            var request = new ScanRequest
            {
                TableName = FollowTableName,
                Limit = 1,
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    { ":f", new AttributeValue { S = followeeId } }
                },
                FilterExpression = "FolloweeId = :f",
                ProjectionExpression = "FolloweeId"
            };

            var response = await _dynamoDbClient.ScanAsync(request);
            return response.Count > 0;
        }
        catch
        {
            return false;
        }
    }

    public Task FollowUserAsync(string userId, string followeeId)
    {
        return _dynamoDbService.FollowUserAsync(userId, followeeId);
    }

    public async Task<int> GetFollowerCountAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId))
            return 0;

        try
        {
            int total = 0;
            Dictionary<string, AttributeValue>? lastKey = null;

            do
            {
                var request = new ScanRequest
                {
                    TableName = FollowTableName,
                    Select = "COUNT",
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                    {
                        { ":f", new AttributeValue { S = userId } }
                    },
                    FilterExpression = "FolloweeId = :f",
                    ExclusiveStartKey = lastKey
                };

                var response = await _dynamoDbClient.ScanAsync(request);
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

    public async Task<(bool success, ProfileResponse? response, string? error)> GetProfileAsync(string userId, string selectedUserId)
    {
        try
        {
            Entities.User user = null;
            var following = false;
                //var user = await _context.Users.Include(u => u.Profile)
            //    .FirstOrDefaultAsync(u => u.Id == userId);
            if (!string.IsNullOrEmpty(selectedUserId) && selectedUserId != "undefined")
            {
                user = await _dynamoDbService.GetUserAsync(selectedUserId);
                following = await IsFollowingAsync(userId, selectedUserId);

            }
            else
            {
                user = await _dynamoDbService.GetUserAsync(userId);
            }

            if (user == null)
            {
                return (false, null, "USER_NOT_FOUND");
            }


            var profile = user.Profile;
            var response = new ProfileResponse
            {
                UserId = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                ProfileImage = profile?.ProfileImageUrl ?? "",
                Website = profile?.Website,
                Bio = profile?.Bio,
                Instagram = profile?.Instagram,
                Twitter = profile?.Twitter,
                Youtube = profile?.Youtube,
                Pinterest = profile?.Pinterest,
                Newsletter = profile?.Newsletter ?? false,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                ProfileImageUrl = user.Profile?.ProfileImageUrl,
                isFollowing = following
            };

            return (true, response, null);
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }

    public async Task<bool> IsFollowingAsync(string followerId, string followeeId)
    {
        if (string.IsNullOrEmpty(followerId) || string.IsNullOrEmpty(followeeId))
            return false;

        try
        {
            var request = new ScanRequest
            {
                TableName = FollowTableName,
                //Limit = 1,
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    { ":follower", new AttributeValue { S = followerId } },
                    { ":followee", new AttributeValue { S = followeeId } }
                },
                FilterExpression = "FollowerId = :follower AND FolloweeId = :followee",
                ProjectionExpression = "FollowerId, FolloweeId"
            };

            var response = await _dynamoDbClient.ScanAsync(request);
            return response.Count > 0;
        }
        catch
        {
            return false;
        }
    }

    public async Task<FollowResult> ToggleFollowAsync(string followerId, string followeeId)
    {
        var result = new FollowResult { IsFollowing = false, FollowerCount = 0 };

        if (string.IsNullOrEmpty(followerId) || string.IsNullOrEmpty(followeeId) || followerId == followeeId)
            return result;

        try
        {
            // Check existing
            var existing = await FindFollowItemAsync(followerId, followeeId);
            if (existing != null)
            {
                // Delete the follow item
                // Attempt to delete using FollowerId/FolloweeId as key attributes (works if those are the PK/ SK)
                var key = new Dictionary<string, AttributeValue>
                {
                    { "FollowerId", new AttributeValue { S = followerId } },
                    { "FolloweeId", new AttributeValue { S = followeeId } }
                };

                try
                {
                    await _dynamoDbClient.DeleteItemAsync(new DeleteItemRequest
                    {
                        TableName = FollowTableName,
                        Key = key
                    });
                }
                catch
                {
                    // If delete by assumed key fails, attempt delete by using the returned item's keys (if present)
                    if (existing.ContainsKey("Id"))
                    {
                        // if table had a single hash key "Id"
                        var altKey = new Dictionary<string, AttributeValue>
                        {
                            { "Id", existing["Id"] }
                        };
                        await _dynamoDbClient.DeleteItemAsync(new DeleteItemRequest
                        {
                            TableName = FollowTableName,
                            Key = altKey
                        });
                    }
                }

                result.IsFollowing = false;
            }
            else
            {
                // Create follow item
                var item = new Dictionary<string, AttributeValue>
                {
                    { "FollowerId", new AttributeValue { S = followerId } },
                    { "FolloweeId", new AttributeValue { S = followeeId } },
                    { "CreatedAt", new AttributeValue { S = DateTime.UtcNow.ToString("o") } }
                };

                await _dynamoDbClient.PutItemAsync(new PutItemRequest
                {
                    TableName = FollowTableName,
                    Item = item
                });

                result.IsFollowing = true;
            }

            result.FollowerCount = await GetFollowerCountAsync(followeeId);
            return result;
        }
        catch
        {
            return result;
        }
    }

    private async Task<Dictionary<string, AttributeValue>?> FindFollowItemAsync(string followerId, string followeeId)
    {
        try
        {
            var request = new ScanRequest
            {
                TableName = FollowTableName,
                Limit = 1,
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    { ":follower", new AttributeValue { S = followerId } },
                    { ":followee", new AttributeValue { S = followeeId } }
                },
                FilterExpression = "FollowerId = :follower AND FolloweeId = :followee"
            };

            var response = await _dynamoDbClient.ScanAsync(request);
            if (response.Count > 0)
                return response.Items.FirstOrDefault();

            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<(bool success, UpdateProfileResponse? response, string? error)> UpdateProfileAsync(string userId, UpdateProfileRequest request)
    {
        try
        {
            //var user = await _context.Users.Include(u => u.Profile)
            //    .FirstOrDefaultAsync(u => u.Id == userId);

            var user = await _dynamoDbService.GetUserAsync(userId);

            if (user == null)
            {
                return (false, null, "USER_NOT_FOUND");
            }

            // Update user
            if (!string.IsNullOrEmpty(request.FirstName))
                user.FirstName = request.FirstName;
            
            if (!string.IsNullOrEmpty(request.LastName))
                user.LastName = request.LastName;
            
            user.UpdatedAt = DateTime.UtcNow;

            // Update profile
            if (user.Profile == null)
            {
                user.Profile = new Entities.UserProfile { UserId = userId };
            }

            if (!string.IsNullOrEmpty(request.Website))
                user.Profile.Website = request.Website;
            
            if (!string.IsNullOrEmpty(request.Bio))
                user.Profile.Bio = request.Bio;
            
            if (request.SocialLinks != null)
            {
                if (!string.IsNullOrEmpty(request.SocialLinks.Instagram))
                    user.Profile.Instagram = request.SocialLinks.Instagram;
                
                if (!string.IsNullOrEmpty(request.SocialLinks.Twitter))
                    user.Profile.Twitter = request.SocialLinks.Twitter;
                
                if (!string.IsNullOrEmpty(request.SocialLinks.Youtube))
                    user.Profile.Youtube = request.SocialLinks.Youtube;
                
                if (!string.IsNullOrEmpty(request.SocialLinks.Pinterest))
                    user.Profile.Pinterest = request.SocialLinks.Pinterest;
            }

            if (request.Newsletter.HasValue)
                user.Profile.Newsletter = request.Newsletter.Value;

            user.Profile.UpdatedAt = DateTime.UtcNow;


            await _dynamoDbService.UpdateUserAsync(user);
           // _context.Users.Update(user);
            //await _context.SaveChangesAsync();

            var response = new UpdateProfileResponse
            {
                UserId = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Website = user.Profile.Website,
                Bio = user.Profile.Bio,
                SocialLinks = new SocialLinksResponse
                {
                    Instagram = user.Profile.Instagram,
                    Twitter = user.Profile.Twitter,
                    Youtube = user.Profile.Youtube,
                    Pinterest = user.Profile.Pinterest
                },
                Newsletter = user.Profile.Newsletter,
                UpdatedAt = user.Profile.UpdatedAt
            };

            return (true, response, null);
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }

    public async Task<(bool success, UploadProfilePictureResponse? response, string? error)> UploadProfilePictureAsync(string userId, IFormFile file)
    {
        try
        {
            //var user = await _context.Users.Include(u => u.Profile)
            //    .FirstOrDefaultAsync(u => u.Id == userId);

            var user = await _dynamoDbService.GetUserAsync(userId);

            if (user == null)
            {
                return (false, null, "USER_NOT_FOUND");
            }

            // Validate file
            if (file.Length > 5 * 1024 * 1024) // 5MB limit
            {
                return (false, null, "FILE_TOO_LARGE");
            }

            if (user.Profile == null)
            {
                user.Profile = new Entities.UserProfile { UserId = userId };
            }

            await using var stream = file.OpenReadStream();
            string ImageUrl = await _s3Service.UploadToS3Async(stream, $"ProfileImage/{file.FileName}", file.ContentType);

            user.Profile.ProfileImageUrl = ImageUrl;
            user.Profile.UpdatedAt = DateTime.UtcNow;

            await _dynamoDbService.UpdateUserAsync(user);
            //_context.Users.Update(user);
            //await _context.SaveChangesAsync();

            var response = new UploadProfilePictureResponse
            {
                UserId = user.Id,
                ProfileImage = ImageUrl,
                UploadedAt = DateTime.UtcNow
            };

            return (true, response, null);
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }

    public async Task<List<ProfileResponse>> GetFollowersAsync(string userId)
    {
        var followers = new List<ProfileResponse>();

        if (string.IsNullOrEmpty(userId))
            return followers;

        try
        {
            Dictionary<string, AttributeValue>? lastKey = null;

            do
            {
                var scanRequest = new ScanRequest
                {
                    TableName = FollowTableName,
                    ProjectionExpression = "FollowerId",
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                    {
                        { ":followee", new AttributeValue { S = userId } }
                    },
                    FilterExpression = "FolloweeId = :followee",
                    ExclusiveStartKey = lastKey
                };

                var scanResponse = await _dynamoDbClient.ScanAsync(scanRequest);

                if (scanResponse.Items != null && scanResponse.Items.Count > 0)
                {
                    foreach (var item in scanResponse.Items)
                    {
                        if (item.TryGetValue("FollowerId", out var followerAttr) && !string.IsNullOrEmpty(followerAttr.S))
                        {
                            var followerId = followerAttr.S;
                            // Get follower user details from Dynamo service
                            var followerUser = await _dynamoDbService.GetUserAsync(followerId);
                            if (followerUser != null)
                            {
                                followers.Add(new ProfileResponse
                                {
                                    UserId = followerUser.Id,
                                    FirstName = followerUser.FirstName,
                                    LastName = followerUser.LastName,
                                    Email = followerUser.Email,
                                    ProfileImage = followerUser.Profile?.ProfileImageUrl ?? "",
                                    ProfileImageUrl = followerUser.Profile?.ProfileImageUrl,
                                    Instagram = followerUser.Profile?.Instagram,
                                    Twitter = followerUser.Profile?.Twitter,
                                    Youtube = followerUser.Profile?.Youtube,
                                    Pinterest = followerUser.Profile?.Pinterest,
                                    CreatedAt = followerUser.CreatedAt,
                                    UpdatedAt = followerUser.UpdatedAt
                                });
                            }
                            else
                            {
                                // If user not found in users table, still include id
                                followers.Add(new ProfileResponse
                                {
                                    UserId = followerId,
                                    FirstName = string.Empty,
                                    LastName = string.Empty,
                                    ProfileImage = string.Empty
                                });
                            }
                        }
                    }
                }

                lastKey = scanResponse.LastEvaluatedKey;
            } while (lastKey != null && lastKey.Count > 0);
        }
        catch
        {
            // swallow and return whatever was collected
        }

        return followers;
    }

    public async Task<List<ProfileResponse>> GetFollowingAsync(string userId)
    {
        var following = new List<ProfileResponse>();

        if (string.IsNullOrEmpty(userId))
            return following;

        try
        {
            Dictionary<string, AttributeValue>? lastKey = null;

            do
            {
                var scanRequest = new ScanRequest
                {
                    TableName = FollowTableName,
                    ProjectionExpression = "FolloweeId",
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                    {
                        { ":follower", new AttributeValue { S = userId } }
                    },
                    FilterExpression = "FollowerId = :follower",
                    ExclusiveStartKey = lastKey
                };

                var scanResponse = await _dynamoDbClient.ScanAsync(scanRequest);

                if (scanResponse.Items != null && scanResponse.Items.Count > 0)
                {
                    foreach (var item in scanResponse.Items)
                    {
                        if (item.TryGetValue("FolloweeId", out var followeeAttr) && !string.IsNullOrEmpty(followeeAttr.S))
                        {
                            var followeeId = followeeAttr.S;
                            var followeeUser = await _dynamoDbService.GetUserAsync(followeeId);
                            if (followeeUser != null)
                            {
                                following.Add(new ProfileResponse
                                {
                                    UserId = followeeUser.Id,
                                    FirstName = followeeUser.FirstName,
                                    LastName = followeeUser.LastName,
                                    Email = followeeUser.Email,
                                    ProfileImage = followeeUser.Profile?.ProfileImageUrl ?? "",
                                    ProfileImageUrl = followeeUser.Profile?.ProfileImageUrl,
                                    Instagram = followeeUser.Profile?.Instagram,
                                    Twitter = followeeUser.Profile?.Twitter,
                                    Youtube = followeeUser.Profile?.Youtube,
                                    Pinterest = followeeUser.Profile?.Pinterest,
                                    CreatedAt = followeeUser.CreatedAt,
                                    UpdatedAt = followeeUser.UpdatedAt
                                });
                            }
                            else
                            {
                                following.Add(new ProfileResponse
                                {
                                    UserId = followeeId,
                                    FirstName = string.Empty,
                                    LastName = string.Empty,
                                    ProfileImage = string.Empty
                                });
                            }
                        }
                    }
                }

                lastKey = scanResponse.LastEvaluatedKey;
            } while (lastKey != null && lastKey.Count > 0);
        }
        catch
        {
            // swallow exception and return what we have
        }

        return following;
    }
    }
