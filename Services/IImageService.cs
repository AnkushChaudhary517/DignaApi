using DignaApi.Models.Requests;
using DignaApi.Models.Responses;

namespace DignaApi.Services;

public interface IImageService
{
    Task<List<ImageListResponse>> GetAllImagesAsync();
    Task UploadImagesAsync(List<UploadImageRequest> images);
    Task<ImageDetailsResponse?> GetImageDetailsAsync(string imageId);
    Task<ImageDownloadResponse?> DownloadImageAsync(string imageId, string sizeId);
}
