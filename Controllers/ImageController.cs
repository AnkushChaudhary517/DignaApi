using DignaApi.Models.Requests;
using DignaApi.Models.Responses;
using DignaApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DignaApi.Controllers;

[ApiController]
[Route("api/v1")]
public class ImageController : ControllerBase
{
    private readonly IImageService _imageService;
    private readonly IS3Service _s3Service;
    private readonly IImageProcessingService _imageProcessingService;

    public ImageController(IImageService imageService, IS3Service s3Service, IImageProcessingService imageProcessingService)
    {
        _imageService = imageService;
        _s3Service = s3Service;
        _imageProcessingService = imageProcessingService;
    }

    [HttpGet("images")]
    public async Task<ActionResult<ApiResponse<List<ImageListResponse>>>> GetAllImages()
    {
        try
        {
            //var images = await _imageService.GetAllImagesAsync();
            var images = await _imageProcessingService.GetAllImagesAsync();
            var res = images?.ConvertAll(i=> new ImageListResponse
            {
                Id = i.Id,
                Title = i.Title,
                ImageUrl = i.ImageUrl,
                Photographer = i.Photographer,
                AspectRatio = i.AspectRatio,
                DownloadCount = i.Downloads,
                Qualityurls = i.QualityUrls != null ? i.QualityUrls.Values.ToList() : new List<string>(),
                DownloadSizes = i.QualityUrls.Select(d => new DownloadSizeResponse
                {
                    Name = d.Key,
                    Url = d.Value
                }).ToList()

            });

            return Ok(new ApiResponse<List<ImageListResponse>>
            {
                Success = true,
                Data = res,
                Message = "Images retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<List<ImageListResponse>>
            {
                Success = false,
                Error = new ErrorDetails
                {
                    Code = "IMAGE_FETCH_ERROR",
                    Message = $"Failed to retrieve images : {ex.ToString()}",
                    StatusCode = 500
                }
            });
        }
    }

    [Authorize]
    [HttpPost("image/like/{id}")]
    public async Task<IActionResult> LikeImage(string id)
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new ApiResponse<object>
                {
                    Success = false,
                    Error = new ErrorDetails
                    {
                        Code = "UNAUTHORIZED",
                        Message = "User not authenticated",
                        StatusCode = 401
                    }
                });
            }
            await _imageProcessingService.LikeImageAsync(userId, id);
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Image liked successfully"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Error = new ErrorDetails
                {
                    Code = "LIKE_ERROR",
                    Message = "Failed to like image",
                    StatusCode = 500
                }
            });
        }
    }

    [HttpGet("image/{id}")]
    public async Task<ActionResult<ApiResponse<ImageDetailsResponse>>> GetImageDetails(string id)
    {
        try
        {
            var image = await _imageProcessingService.GetImageFromId(id);
            //var image = await _imageService.GetImageDetailsAsync(id);
            if (image == null)
            {
                return NotFound(new ApiResponse<ImageDetailsResponse>
                {
                    Success = false,
                    Error = new ErrorDetails
                    {
                        Code = "IMAGE_NOT_FOUND",
                        Message = "Image not found",
                        StatusCode = 404
                    }
                });
            }
            var res = new ImageDetailsResponse
            {
                Id = image.Id,
                Title = image.Title,
                Description = image.Description,
                ImageUrl = image.ImageUrl,
                Photographer = image.Photographer,
                AspectRatio = image.AspectRatio,
                DownloadCount = image.Downloads,
                DownloadSizes = image.QualityUrls.Select(d => new DownloadSizeResponse
                {
                    Name = d.Key,
                    Url = d.Value
                }).ToList()
            };

            return Ok(new ApiResponse<ImageDetailsResponse>
            {
                Success = true,
                Data = res,
                Message = "Image details retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<ImageDetailsResponse>
            {
                Success = false,
                Error = new ErrorDetails
                {
                    Code = "IMAGE_DETAILS_ERROR",
                    Message = "Failed to retrieve image details",
                    StatusCode = 500
                }
            });
        }
    }

    [HttpPost("{id}/download")]
    public async Task<ActionResult<ApiResponse<ImageDownloadResponse>>> DownloadImage(string id, [FromBody] DownloadImageRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.SizeId))
            {
                return BadRequest(new ApiResponse<ImageDownloadResponse>
                {
                    Success = false,
                    Error = new ErrorDetails
                    {
                        Code = "INVALID_SIZE",
                        Message = "Size ID is required",
                        StatusCode = 400
                    }
                });
            }

            var downloadInfo = await _imageService.DownloadImageAsync(id, request.SizeId);
            if (downloadInfo == null)
            {
                return NotFound(new ApiResponse<ImageDownloadResponse>
                {
                    Success = false,
                    Error = new ErrorDetails
                    {
                        Code = "DOWNLOAD_NOT_FOUND",
                        Message = "Image or size not found",
                        StatusCode = 404
                    }
                });
            }

            return Ok(new ApiResponse<ImageDownloadResponse>
            {
                Success = true,
                Data = downloadInfo,
                Message = "Download initiated successfully"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<ImageDownloadResponse>
            {
                Success = false,
                Error = new ErrorDetails
                {
                    Code = "DOWNLOAD_ERROR",
                    Message = "Failed to process download",
                    StatusCode = 500
                }
            });
        }
    }
    [HttpPost("profile/uploads")]
    public async Task<IActionResult> UploadFiles(
            [FromForm] List<IFormFile> files,
            [FromForm] List<FileMetaData> meta)
    {
        if (files == null || files.Count == 0)
            return BadRequest("No files uploaded.");

        if (meta == null || meta.Count != files.Count)
            return BadRequest("Meta information missing or does not match file count.");

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var uploadResults = new List<UploadImageRequest>();

        for (int i = 0; i < files.Count; i++)
        {
            var file = files[i];
            var metadata = meta[i];
            if (metadata == null)
                return BadRequest($"Meta information missing for file : {file.FileName}");

            var image = new DignaApi.Entities.DynamoEntitites.Image
            {
                Title = file.FileName,
                Description = metadata.Description,
                Photographer = metadata.Photographer,
                Tags = metadata.Tags?.Split(',')?.ToList(),
                Visibility = "public",
                UserId = userId!,
                Aperture = metadata.Aperture,
                Location = metadata.Location,
                Focal = metadata.Focal,
                Camera = metadata.Camera,
                ISO = metadata.ISO,
                CameraModel = metadata.CameraModel,
                PublishAndDistributePermission = metadata.PublishAndDistributePermission,
                RightsOwned = metadata.RightsOwned,
                Downloads = 0,
                Id = Guid.NewGuid().ToString(),
                Likes = 0,
                QualityUrls = new Dictionary<string, string>(),
                UpdatedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                
            };
            // Example: Save file locally (change path as needed)
            //var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
            //if (!Directory.Exists(uploadsPath))
            //    Directory.CreateDirectory(uploadsPath);

            //var filePath = Path.Combine(uploadsPath, file.FileName);

            using (var stream = file.OpenReadStream())
            {
                var dict =await _imageProcessingService.UploadImageWithQualitiesAsync(stream,image);
                dict?.Keys?.ToList()?.ForEach(k =>
                {
                    uploadResults.Add(new UploadImageRequest()
                    {
                        ImageUrl = dict[k],
                        CreatedAt = DateTime.UtcNow,
                        Title = k,
                        //FileName = file.FileName,
                        //metadata.Width,
                        //metadata.Height,
                        //metadata.LowQuality,
                        //Path = $"/uploads/{file.FileName}"
                    });
                });
                //var ImageUrl = await _s3Service.UploadToS3Async(stream, file.FileName, file.ContentType);
                //uploadResults.Add(new UploadImageRequest()
                //{
                //    ImageUrl = ImageUrl,
                //    CreatedAt = DateTime.UtcNow,
                //    Title = file.FileName,
                //    //FileName = file.FileName,
                //    //metadata.Width,
                //    //metadata.Height,
                //    //metadata.LowQuality,
                //    //Path = $"/uploads/{file.FileName}"
                //});
            }

            
        }
        //TODO:Needs to be removed
        //await _imageService.UploadImagesAsync(uploadResults);

        return Ok(new
        {
            Message = "Files uploaded successfully!",
            Files = uploadResults
        });
    }


    [HttpGet("/imagesFromTag/{tag}")]
    public async Task<ActionResult<ApiResponse<List<ImageListResponse>>>> GetImagesFromTag(string tag)
    {
        try
        {
            var listimages = await _imageProcessingService.GetImagesFromTagAsync(tag);
            var list = listimages?.Select(i => new ImageListResponse
            {
                Id = i.Id,
                Title = i.Title,
                ImageUrl = i.ImageUrl,
                Photographer = i.Photographer,
                AspectRatio = i.AspectRatio,
                DownloadCount = i.Downloads,
                Qualityurls = i.QualityUrls != null ? i.QualityUrls.Values.ToList() : new List<string>()
            }).ToList();
            return Ok(new ApiResponse<List<ImageListResponse>>
            {
                Success = true,
                Data = list,
                Message = "Images retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<List<ImageListResponse>>
            {
                Success = false,
                Error = new ErrorDetails
                {
                    Code = "IMAGE_FETCH_ERROR",
                    Message = $"Failed to retrieve images : {ex.ToString()}",
                    StatusCode = 500
                }
            });
        }
    }
}
public class FileMetaData
{
    public string? Tags { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; } = string.Empty;
    public string? Camera { get; set; } = string.Empty;
    public string? Aperture { get; set; } = string.Empty;
    public string? Location { get; set; } = string.Empty;
    public string? ISO { get; set; }
    public string? CameraModel { get; set; } = string.Empty;
    public string? Photographer { get; set; }
    public string? Focal { get; set; } = string.Empty;

    public bool RightsOwned { get; set; } = false;

    public bool PublishAndDistributePermission { get; set; } = false;
}

