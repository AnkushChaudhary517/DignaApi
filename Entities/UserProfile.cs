namespace DignaApi.Entities;

public class UserProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public string? ProfileImageUrl { get; set; }
    public string? Bio { get; set; }
    public string? Website { get; set; }
    public string? Instagram { get; set; }
    public string? Twitter { get; set; }
    public string? Youtube { get; set; }
    public string? Pinterest { get; set; }
    public bool Newsletter { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public virtual User? User { get; set; }
}
