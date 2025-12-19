namespace DignaApi.Models
{
    public class FollowResult
    {
        public bool IsFollowing { get; set; }
        public int FollowerCount { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
