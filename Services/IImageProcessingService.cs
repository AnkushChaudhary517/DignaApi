using DignaApi.Entities.DynamoEntitites;
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
    }
}
