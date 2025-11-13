namespace DignaApi.Models
{
    public class UploadItem
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public string ImageUrl { get; set; } = "";
        public int Downloads { get; set; }
        public string CreatedAt { get; set; } = "";
    }
}
