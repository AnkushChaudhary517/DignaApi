namespace DignaApi.Models.Responses
{

    public class UserStatsResponse
    {
        public bool Success { get; set; }
        public UserStatsData Data { get; set; } = new();
    }

    public class UserStatsData
    {
        public UploadData Uploads { get; set; } = new();
        public DownloadData Downloads { get; set; } = new();
        public FollowerData Followers { get; set; } = new();
        public FollowingData Following { get; set; } = new();
        public StatsInfo Stats { get; set; } = new();
    }

    public class UploadData
    {
        public int Count { get; set; }
        public List<UploadItem> Items { get; set; } = new();
    }

    public class DownloadData
    {
        public int Count { get; set; }
        public List<DownloadItem> Items { get; set; } = new();
    }

    public class FollowerData
    {
        public int Count { get; set; }
        public List<FollowerItem> Items { get; set; } = new();
    }

    public class FollowingData
    {
        public int Count { get; set; }
        public List<FollowingItem> Items { get; set; } = new();
    }
}
