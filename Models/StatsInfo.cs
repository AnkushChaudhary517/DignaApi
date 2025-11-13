namespace DignaApi.Models
{
    public class StatsInfo
    {
        public int TotalUploads { get; set; }
        public int TotalDownloads { get; set; }
        public int TotalFollowers { get; set; }
        public int TotalFollowing { get; set; }
        public int AverageDownloadsPerImage { get; set; }
        public PopularImage MostPopularImage { get; set; } = new();
    }

    public class PopularImage
    {
        public string Title { get; set; } = "";
        public int Downloads { get; set; }
    }
}
