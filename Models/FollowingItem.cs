namespace DignaApi.Models
{
    public class FollowingItem
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Username { get; set; } = "";
        public string ProfileImage { get; set; } = "";
        public string Bio { get; set; } = "";
        public int Followers { get; set; }
    }
}
