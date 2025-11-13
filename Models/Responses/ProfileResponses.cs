namespace DignaApi.Models.Responses;

public class ProfileResponse
{
    public string UserId { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string ProfileImage { get; set; } = string.Empty;
    public string? Website { get; set; }
    public string? Bio { get; set; }
    public SocialLinksResponse SocialLinks { get; set; } = new();
    public bool Newsletter { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? Instagram { get; internal set; }
    public string? Twitter { get; internal set; }
    public string? Youtube { get; internal set; }
    public string? Pinterest { get; internal set; }
    public string? ProfileImageUrl { get; internal set; }
}

public class SocialLinksResponse
{
    public string? Instagram { get; set; }
    public string? Twitter { get; set; }
    public string? Youtube { get; set; }
    public string? Pinterest { get; set; }
}

public class UpdateProfileResponse
{
    public string UserId { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Website { get; set; }
    public string? Bio { get; set; }
    public SocialLinksResponse SocialLinks { get; set; } = new();
    public bool Newsletter { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class UploadProfilePictureResponse
{
    public string UserId { get; set; } = string.Empty;
    public string ProfileImage { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
}
