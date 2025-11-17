using DignaApi.Models;
using DignaApi.Models.Requests;
using DignaApi.Models.Responses;
using DignaApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DignaApi.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/profile")]
public class ProfileController : ControllerBase
{
    private readonly IProfileService _profileService;
    //private readonly IImageService _imageService;
    private readonly IImageProcessingService _imageProcessingService;

    public ProfileController(IProfileService profileService, IImageService imageService, IImageProcessingService imageProcessingService)
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

        if(!string.IsNullOrEmpty(id) && id.ToLower() != "undefined")
        {
            userId = id;
        }
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ApiResponseHelper.Error<ProfileResponse>(
                "UNAUTHORIZED",
                "User not authenticated",
                401
            ));
        }

        var (success, response, error) = await _profileService.GetProfileAsync(userId);

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
            // 1️⃣ Save image if provided
            string imageUrl = null;
            //if (model.ProfileImage != null && model.ProfileImage.Length > 0)
            //{
            //    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
            //    if (!Directory.Exists(uploadsFolder))
            //        Directory.CreateDirectory(uploadsFolder);

            //    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(model.ProfileImage.FileName)}";
            //    var filePath = Path.Combine(uploadsFolder, fileName);

            //    using (var stream = new FileStream(filePath, FileMode.Create))
            //    {
            //        await model.ProfileImage.CopyToAsync(stream);
            //    }

            //    imageUrl = $"/uploads/{fileName}";
            //}

            // 2️⃣ Simulate saving other details (to DB, for example)
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
            // TODO: Save userProfile to database

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
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        List<Entities.DynamoEntitites.Image> likedImages = null;
        if (!string.IsNullOrEmpty(userId))
        {
            likedImages = await _imageProcessingService.GetImagesLikedByuserAsync(userId);
            //get all images for user including private
        }
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
        var uploadedImages = await _imageProcessingService.GetImagesByUserid(userId);
        UploadData data = new UploadData();
        uploadedImages?.ForEach(i =>
        {
            var liked = false;
            if (likedImages != null && likedImages.Any(x => x.Id == i.Id))
                liked = true;
            data.Items.Add(new UploadItem()
            {
                Id = i.Id,
                ImageUrl = i.ImageUrl,
                Title = i.Title,
                Liked = liked,

            });
        });
        data.Count = uploadedImages.Count;


        var response = new UserStatsResponse
        {
            Success = true
        };
        response.Data.Uploads = data;

        return Ok(response);
    }
}
