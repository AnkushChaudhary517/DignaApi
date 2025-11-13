using Microsoft.EntityFrameworkCore;
using DignaApi.Data;
using DignaApi.Models.Responses;
using DignaApi.Models.Requests;

namespace DignaApi.Services;

public class ImageService : IImageService
{
    private readonly DignaDbContext _context;

    public ImageService(DignaDbContext context)
    {
        _context = context;
    }

    public async Task<List<ImageListResponse>> GetAllImagesAsync()
    {
        var images = await _context.Images.ToListAsync();
        return images.Select(i => new ImageListResponse
        {
            Id = i.Id,
            Title = i.Title,
            ImageUrl = i.ImageUrl,
            Photographer = i.Photographer,
            AspectRatio = i.AspectRatio,
            DownloadCount = i.Downloads
        }).ToList();
    }

    public async Task<ImageDetailsResponse?> GetImageDetailsAsync(string imageId)
    {
        var image = await _context.Images
            //.Include(i => i.DownloadSizes)
            .FirstOrDefaultAsync(i => i.Id == imageId);

        if (image == null)
            return null;

        return new ImageDetailsResponse
        {
            Id = image.Id,
            Title = image.Title,
            Description = image.Description,
            ImageUrl = image.ImageUrl,
            Photographer = image.Photographer,
            AspectRatio = image.AspectRatio,
            DownloadCount = image.Downloads,
            //DownloadSizes = image.DownloadSizes.Select(d => new DownloadSizeResponse
            //{
            //    Id = d.Id,
            //    Name = d.Name,
            //    Width = d.Width,
            //    Height = d.Height,
            //    FileSizeBytes = d.FileSizeBytes
            //}).ToList()
        };
    }

    public async Task<ImageDownloadResponse?> DownloadImageAsync(string imageId, string sizeId)
    {
        var image = await _context.Images.FirstOrDefaultAsync(i => i.Id == imageId);
        if (image == null)
            return null;

        var size = await _context.ImageDownloadSizes
            .FirstOrDefaultAsync(s => s.Id == sizeId && s.ImageId == imageId);
        if (size == null)
            return null;

        // Increment download count
        image.Downloads++;
        await _context.SaveChangesAsync();

        // Generate download response
        return new ImageDownloadResponse
        {
            DownloadUrl = image.ImageUrl,
            FileName = $"{image.Title.Replace(" ", "_")}_{size.Name}_{size.Width}x{size.Height}.jpg",
            ContentType = "image/jpeg"
        };
    }

    public async Task UploadImagesAsync(List<UploadImageRequest> images)
    {
        images?.ForEach(image =>
        {
            _context.Images.Add(new Entities.DynamoEntitites.Image()
            {
                AspectRatio = image.AspectRatio,
                Description = image.Description,
                CreatedAt = DateTime.UtcNow,
                Photographer = image.Photographer,
                Title = image.Title,
                Downloads = 0,
                ImageUrl = image.ImageUrl,
                UpdatedAt = DateTime.UtcNow
                

            });

        });
        await _context.SaveChangesAsync();
    }
}
