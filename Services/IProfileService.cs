using DignaApi.Models.Requests;
using DignaApi.Models.Responses;

namespace DignaApi.Services;

public interface IProfileService
{
    Task FollowUserAsync(string userId, string followeeId);
    Task<(bool success, ProfileResponse? response, string? error)> GetProfileAsync(string userId);
    Task<(bool success, UpdateProfileResponse? response, string? error)> UpdateProfileAsync(string userId, UpdateProfileRequest request);
    Task<(bool success, UploadProfilePictureResponse? response, string? error)> UploadProfilePictureAsync(string userId, IFormFile file);
}
