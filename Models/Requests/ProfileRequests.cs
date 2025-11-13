using Microsoft.AspNetCore.Mvc;

namespace DignaApi.Models.Requests;

public class UpdateProfileRequest
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Website { get; set; }
    public string? Bio { get; set; }
    public SocialLinksDto? SocialLinks { get; set; }
    public bool? Newsletter { get; set; }
    public string ImageUrl { get; internal set; }
}
public class ProfileUpdateRequest
{
    [FromForm(Name = "firstName")]
    public string? FirstName { get; set; }

    [FromForm(Name = "lastName")]
    public string? LastName { get; set; }

    [FromForm(Name = "email")]
    public string? Email { get; set; }

    [FromForm(Name = "website")]
    public string? Website { get; set; }

    [FromForm(Name = "instagram")]
    public string? Instagram { get; set; }

    [FromForm(Name = "twitter")]
    public string? Twitter { get; set; }

    [FromForm(Name = "youtube")]
    public string? Youtube { get; set; }

    [FromForm(Name = "pinterest")]
    public string? Pinterest { get; set; }

    [FromForm(Name = "bio")]
    public string? Bio { get; set; }

    [FromForm(Name = "password")]
    public string? Password { get; set; }

    [FromForm(Name = "newsletter")]
    public bool Newsletter { get; set; }

    [FromForm(Name = "profileImage")]
    public IFormFile? ProfileImage { get; set; }
}


public class SocialLinksDto
{
    public string? Instagram { get; set; }
    public string? Twitter { get; set; }
    public string? Youtube { get; set; }
    public string? Pinterest { get; set; }
}

public class UploadProfilePictureRequest
{
    public IFormFile? File { get; set; }
}
