using Amazon.S3.Model;
using DignaApi.Entities;
using DignaApi.Models;
using DignaApi.Models.Requests;
using DignaApi.Models.Responses;
using DignaApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DignaApi.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/profile")]
public class ProfileController : ControllerBase
{
    private readonly IProfileService _profileService;
    //private readonly IImageService _imageService;
    private readonly IImageProcessingService _imageProcessingService;

    public ProfileController(
        IProfileService profileService,
        IImageService imageService,
        IImageProcessingService imageProcessingService)
    {
        _profileService = profileService;
        //_imageService = imageService;
        _imageProcessingService = imageProcessingService;
    }

    [HttpGet]
    [Route("{id}")]
    public async Task<ActionResult<ApiResponse<ProfileResponse>>> GetProfile(string id)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        //if(!string.IsNullOrEmpty(id) && id.ToLower() != "undefined")
        //{
        //    userId = id;
        //}
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ApiResponseHelper.Error<ProfileResponse>(
                "UNAUTHORIZED",
                "User not authenticated",
                401
            ));
        }

        var (success, response, error) = await _profileService.GetProfileAsync(userId,id);

        if (!success)
        {
            return NotFound(ApiResponseHelper.Error<ProfileResponse>(
                error ?? "PROFILE_NOT_FOUND",
                "Profile not found",
                404
            ));
        }

        return Ok(ApiResponseHelper.Success(response));
    }

    [HttpPost("follow")]
    public async Task<IActionResult> FollowUser([FromForm] FollowUserRequest model)
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponseHelper.Error<ProfileResponse>(
                    "UNAUTHORIZED",
                    "User not authenticated",
                    401
                ));
            }

            await _profileService.FollowUserAsync(userId, model.FolloweeId);

            return Ok(new
            {
                success = true,
                message = $"Followed {model.FolloweeId} successfully"
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("update")]
    public async Task<IActionResult> UpdateProfile([FromForm] ProfileUpdateRequest model)
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponseHelper.Error<ProfileResponse>(
                    "UNAUTHORIZED",
                    "User not authenticated",
                    401
                ));
            }

            var userProfile = new UpdateProfileRequest()
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                Bio = model.Bio,
                Newsletter = model.Newsletter,
                Website = model.Website,
                SocialLinks = new SocialLinksDto()
                {
                    Instagram = model.Instagram,
                    Pinterest = model.Pinterest,
                    Twitter = model.Twitter,
                    Youtube = model.Youtube
                }
            };

            if (model.ProfileImage != null && model.ProfileImage.Length > 0)
                await _profileService.UploadProfilePictureAsync(userId, model.ProfileImage);
            await _profileService.UpdateProfileAsync(userId, userProfile);

            return Ok(new
            {
                success = true,
                message = "Profile updated successfully",
                data = userProfile
            });
        }
        catch (Exception ex)            
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPut]
    public async Task<ActionResult<ApiResponse<UpdateProfileResponse>>> UpdateProfile(UpdateProfileRequest request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ApiResponseHelper.Error<UpdateProfileResponse>(
                "UNAUTHORIZED",
                "User not authenticated",
                401
            ));
        }

        var (success, response, error) = await _profileService.UpdateProfileAsync(userId, request);

        if (!success)
        {
            return BadRequest(ApiResponseHelper.Error<UpdateProfileResponse>(
                error ?? "UPDATE_FAILED",
                error == "USER_NOT_FOUND" 
                    ? "User not found" 
                    : "Failed to update profile",
                400
            ));
        }

        return Ok(ApiResponseHelper.Success(response, "Profile updated successfully"));
    }

    [HttpPost("upload-picture")]
    public async Task<ActionResult<ApiResponse<UploadProfilePictureResponse>>> UploadProfilePicture([FromForm] IFormFile? file)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ApiResponseHelper.Error<UploadProfilePictureResponse>(
                "UNAUTHORIZED",
                "User not authenticated",
                401
            ));
        }

        if (file == null || file.Length == 0)
        {
            return BadRequest(ApiResponseHelper.Error<UploadProfilePictureResponse>(
                "NO_FILE",
                "No file provided",
                400
            ));
        }

        var (success, response, error) = await _profileService.UploadProfilePictureAsync(userId, file);

        if (!success)
        {
            return BadRequest(ApiResponseHelper.Error<UploadProfilePictureResponse>(
                error ?? "UPLOAD_FAILED",
                error == "FILE_TOO_LARGE"
                    ? "File size must be less than 5MB"
                    : "Failed to upload profile picture",
                error == "FILE_TOO_LARGE" ? 413 : 400
            ));
        }

        return Ok(ApiResponseHelper.Success(response, "Profile picture uploaded successfully"));
    }

    [HttpGet("stats/{id}")]
    public async Task<IActionResult> GetUserStats(string id)
    {
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        List<Entities.DynamoEntitites.Image> likedImages = null;
        if (!string.IsNullOrEmpty(currentUserId))
        {
            likedImages = await _imageProcessingService.GetImagesLikedByuserAsync(currentUserId);
        }

        var userId = currentUserId;
        if (!string.IsNullOrEmpty(id) && id.ToLower() != "undefined")
        {
            userId = id;
        }

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ApiResponseHelper.Error<UploadProfilePictureResponse>(
                "UNAUTHORIZED",
                "User not authenticated",
                401
            ));
        }

        // uploaded images (owner's uploads)
        var uploadedImages = await _imageProcessingService.GetImagesByUserid(userId);
        var uploadsResponse = new List<object>();
        foreach (var img in uploadedImages ?? new List<Entities.DynamoEntitites.Image>())
        {
            var liked = likedImages != null && likedImages.Any(x => x.Id == img.Id);

            uploadsResponse.Add(new
            {
                id = img.Id,
                imageUrl = img.ImageUrl,
                title = img.Title,
                liked
            });
        }

        // followers + followerCount
        var followers = await _profileService.GetFollowersAsync(userId);
        var followerCount = followers?.Count ?? await _profileService.GetFollowerCountAsync(userId);
        followers?.ForEach(async f =>
        {
            var u = await _imageProcessingService.GetImagesByUserid(f.UserId);
            if(u != null && u.Any())
                f.Uploads = u.Count;
        });

        // following list + followingCount
        var following = await _profileService.GetFollowingAsync(userId);
        following?.ForEach(async f =>
        {
            var u = await _imageProcessingService.GetImagesByUserid(f.UserId);
            if (u != null && u.Any())
                f.Uploads = u.Count;
        });
        var followingCount = following?.Count ?? 0;

        var currentUserIsFollowing = false;
        if (!string.IsNullOrEmpty(currentUserId))
        {
            currentUserIsFollowing = await _profileService.IsFollowingAsync(currentUserId, userId);
        }

        // downloads BY THE PROFILE USER (images the profile user has downloaded)
        var userDownloadCount = await _imageProcessingService.GetDownloadCountByUserIdAsync(userId);
        var userRecentDownloads = await _imageProcessingService.GetDownloadsByUserIdAsync(userId, 20);
        var downloadsResponse = userRecentDownloads?.Select(d => new
        {
            id = d.Id,
            imageId = d.ImageId,
            imageUrl = d.ImageUrl,
            photographer = d.Photographer,
            sizeId = d.SizeId,
            createdAt = d.CreatedAt
        });

        // build response payload
        var responsePayload = new
        {
            success = true,
            data = new
            {
                uploads = new
                {
                    items = uploadsResponse,
                    count = uploadedImages?.Count ?? 0
                },
                followers = followers?.Select(f => new
                {
                    userId = f.UserId,
                    firstName = f.FirstName,
                    lastName = f.LastName,
                    profileImage = f.ProfileImage,
                    instagram = f.Instagram,
                    twitter = f.Twitter,
                    youtube = f.Youtube,
                    pinterest = f.Pinterest,
                    username = f.FirstName + "." + f.LastName,
                    uploads = f.Uploads
                }),
                followerCount,
                following = following?.Select(f => new
                {
                    userId = f.UserId,
                    firstName = f.FirstName,
                    lastName = f.LastName,
                    profileImage = f.ProfileImage,
                    instagram = f.Instagram,
                    twitter = f.Twitter,
                    youtube = f.Youtube,
                    pinterest = f.Pinterest,
                    username = f.FirstName + "." + f.LastName,
                    uploads = f.Uploads
                }),
                followingCount,
                isFollowing = currentUserIsFollowing,
                downloads = new
                {
                    count = userDownloadCount,
                    recent = downloadsResponse
                }
            }
        };

        return Ok(responsePayload);
    }

    /// <summary>
    /// Follow or unfollow a user
    /// </summary>
    /// <param name="followeeId">The ID of the user to follow/unfollow</param>
    /// <returns>Success response with follow status</returns>
    [HttpPost("followUser/{followeeId}")]
    public async Task<IActionResult> FollowUser(string followeeId)
    {
        try
        {
            // Get current user ID from JWT token
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("userId")?.Value
                ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized(new ApiResponse<object>
                {
                    Success = false,
                    Error = new Models.Responses.ErrorDetails
                    {
                        Code = "UNAUTHORIZED",
                        Message = "User ID not found in token",
                        StatusCode = 401
                    }
                });
            }

            // Prevent users from following themselves
            if (currentUserId == followeeId)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Error = new Models.Responses.ErrorDetails
                    {
                        Code = "INVALID_OPERATION",
                        Message = "You cannot follow yourself",
                        StatusCode = 400
                    }
                });
            }

            // Toggle follow status
            var result = await _profileService.ToggleFollowAsync(currentUserId, followeeId);

            return Ok(new ApiResponse<FollowResult>
            {
                Success = true,
                Data = new FollowResult
                {
                    IsFollowing = result.IsFollowing,
                    Message = result.IsFollowing ? "Successfully followed user" : "Successfully unfollowed user",
                    FollowerCount = result.FollowerCount
                },
                Message = result.IsFollowing ? "User followed successfully" : "User unfollowed successfully"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Error = new Models.Responses.ErrorDetails
                {
                    Code = "INTERNAL_ERROR",
                    Message = "An error occurred while processing your request",
                    StatusCode = 500
                }
            });
        }
    }
}
