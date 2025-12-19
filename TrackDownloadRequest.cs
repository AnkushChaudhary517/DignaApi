namespace DignaApi.Models.Requests;

public class TrackDownloadRequest
{
    public string Title { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string Photographer { get; set; } = string.Empty;
    public string SizeId { get; set; } = string.Empty;
}