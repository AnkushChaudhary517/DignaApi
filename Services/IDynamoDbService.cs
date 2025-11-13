using DignaApi.Entities;
using DignaApi.Entities.DynamoEntitites;
using DignaApi.Models.Responses;

namespace DignaApi.Services
{
    public interface IDynamoDbService
    {
        Task CreateUserAsync(User user);
        Task<User?> GetUserAsync(string userId);
        Task<List<User>> GetAllUsersAsync();
        Task UpdateUserAsync(User user);
        Task DeleteUserAsync(string userId);
        Task<User?> GetUserByEmail(string email);
        Task SaveTagsAsync(Image image);
        Task<List<Image>> SearchImagesByTagAsync(string tag);
        Task<Image> SearchImageByIdAsync(string id);
        Task FollowUserAsync(string userId, string followeeId);
        Task LikeImageAsync(string userId, string imageId);
        Task<List<Image>> GetAllImagesForUserAsync(string userid = null);
    }
}
