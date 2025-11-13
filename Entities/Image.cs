using DignaApi.Entities.DynamoEntitites;

namespace DignaApi.Entities;

public class TestImage
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Photographer { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string AspectRatio { get; set; } = "square"; // "square", "tall", "wide"
    public int DownloadCount { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ImageDownloadSize> DownloadSizes { get; set; } = new List<ImageDownloadSize>();
}

public class ImageDownloadSize
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ImageId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty; // "Small", "Medium", "Large", "Original"
    public int Width { get; set; }
    public int Height { get; set; }
    public long FileSizeBytes { get; set; } // Size in bytes
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Image? Image { get; set; }
}
