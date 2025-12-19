using DignaApi.Models;
using DignaApi.Models.Requests;
using DignaApi.Models.Responses;
using Microsoft.AspNetCore.Http;

namespace DignaApi.Services;

public interface IProfileService
{
    Task<FollowResult> ToggleFollowAsync(string followerId, string followeeId);
    Task<bool> IsFollowingAsync(string followerId, string followeeId);
    Task<int> GetFollowerCountAsync(string userId);
    Task<bool> FollowerExistsAsync(string followeeId);
    Task FollowUserAsync(string userId, string followeeId);
    Task<List<ProfileResponse>> GetFollowersAsync(string userId);
    Task<List<ProfileResponse>> GetFollowingAsync(string userId);
    Task<(bool success, ProfileResponse? response, string? error)> GetProfileAsync(string userId, string selectedUserId);
    Task<(bool success, UpdateProfileResponse? response, string? error)> UpdateProfileAsync(string userId, UpdateProfileRequest request);
    Task<(bool success, UploadProfilePictureResponse? response, string? error)> UploadProfilePictureAsync(string userId, IFormFile file);
}
