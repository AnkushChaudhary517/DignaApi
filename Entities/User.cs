using Amazon.DynamoDBv2.DataModel;

namespace DignaApi.Entities;

[DynamoDBTable("User")]
public class User
{
    [DynamoDBHashKey]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    //[DynamoDBRangeKey] // Sort key
    [DynamoDBProperty]
    public string Email { get; set; }
    [DynamoDBProperty]
    public string FirstName { get; set; } = string.Empty;
    [DynamoDBProperty]
    public string LastName { get; set; } = string.Empty;
    [DynamoDBProperty]
    public string PasswordHash { get; set; } = string.Empty;
    [DynamoDBProperty]
    public bool EmailVerified { get; set; } = false;

    [DynamoDBProperty]
    public int Followers { get; set; }

    public string? VerificationToken { get; set; }
    public string? ResetToken { get; set; }
    public DateTime? ResetTokenExpiry { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public virtual UserProfile? Profile { get; set; }
}
