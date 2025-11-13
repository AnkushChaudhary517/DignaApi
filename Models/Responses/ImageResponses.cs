namespace DignaApi.Models.Responses;

public class ImageListResponse
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string Photographer { get; set; } = string.Empty;
    public string AspectRatio { get; set; } = "square";
    public int DownloadCount { get; set; }
    public List<string> Qualityurls { get; set; } = new();
    public List<DownloadSizeResponse> DownloadSizes { get; set; } = new List<DownloadSizeResponse>();
}

public class ImageDetailsResponse
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string Photographer { get; set; } = string.Empty;
    public string AspectRatio { get; set; } = "square";
    
    public int DownloadCount { get; set; }
    public List<DownloadSizeResponse> DownloadSizes { get; set; } = new();
}

public class DownloadSizeResponse
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
    public long FileSizeBytes { get; set; }
    public string Url { get; set; } = string.Empty;
}

public class ImageDownloadResponse
{
    public string DownloadUrl { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "image/jpeg";
}
