using Microsoft.EntityFrameworkCore;
using DignaApi.Data;
using DignaApi.Models.Requests;
using DignaApi.Models.Responses;

namespace DignaApi.Services;

public class ProfileService : IProfileService
{
    private readonly DignaDbContext _context;
    private readonly IS3Service _s3Service;
    private readonly IDynamoDbService _dynamoDbService;

    public ProfileService(DignaDbContext context, IS3Service s3Service, IDynamoDbService dynamoDbService)
    {
        _context = context;
        _s3Service = s3Service;
        _dynamoDbService = dynamoDbService;
    }

    public Task FollowUserAsync(string userId, string followeeId)
    {
        return _dynamoDbService.FollowUserAsync(userId, followeeId);
    }

    public async Task<(bool success, ProfileResponse? response, string? error)> GetProfileAsync(string userId)
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
            };

            return (true, response, null);
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }

    public async Task<(bool success, UpdateProfileResponse? response, string? error)> UpdateProfileAsync(string userId, UpdateProfileRequest request)
    {
        try
        {
            var user = await _context.Users.Include(u => u.Profile)
                .FirstOrDefaultAsync(u => u.Id == userId);

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

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

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
}
