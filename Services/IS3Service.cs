namespace DignaApi.Services
{
    public interface IS3Service
    {
        Task<string> UploadToS3Async(Stream fileStream, string fileName, string contentType);
        Task<string> UploadToCdnAsync(Stream fileStream, string fileName, string contentType);
    }
}
