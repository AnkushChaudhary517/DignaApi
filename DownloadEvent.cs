using System;

namespace DignaApi.Entities;

public class DownloadEvent
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ImageId { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string Photographer { get; set; } = string.Empty;
    public string SizeId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}