using DignaApi.Entities;
using DignaApi.Entities.DynamoEntitites;
using DignaApi.Models.Requests;
using DignaApi.Models.Responses;

namespace DignaApi.Services
{
    public interface IImageProcessingService
    {
        Task<Dictionary<string, string>> UploadImageWithQualitiesAsync(Stream originalImageStream,Image image);

        Task<List<Image>> GetImagesFromTagAsync(string tag);

        Task<Image> GetImageFromId(string imageId);
        Task LikeImageAsync(string userId, string id);
        Task<List<DignaApi.Entities.DynamoEntitites.Image>> GetAllImagesAsync(string userid = null);
        Task<List<DignaApi.Entities.DynamoEntitites.Image>> SearchImagesAsync(string q);
        Task<List<DignaApi.Entities.DynamoEntitites.Image>> GetImagesByUserid(string userId);
        Task<List<Entities.DynamoEntitites.Image>> GetImagesLikedByuserAsync(string userId);
        Task<int> GetDownloadCountByImageIdAsync(string imageId);
        Task<List<DownloadEvent>> GetDownloadsByImageIdAsync(string imageId, int limit = 50);
        Task<DownloadEvent> TrackDownloadAsync(string imageId, string? userId, TrackDownloadRequest request, string? userAgent = null, string? referer = null);
        Task<int> GetDownloadCountByUserIdAsync(string userId);
        Task<List<DownloadEvent>> GetDownloadsByUserIdAsync(string userId, int limit = 50);
    }
}
