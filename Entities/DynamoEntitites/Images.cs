namespace DignaApi.Entities.DynamoEntitites
{
    using Amazon.DynamoDBv2.DataModel;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations.Schema;

    [DynamoDBTable("Images")]
    public class Image
    {
        

        [DynamoDBHashKey]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [DynamoDBProperty]
        public string UserId { get; set; } = string.Empty;
        [DynamoDBProperty]
        public string Title { get; set; } = string.Empty;
        [DynamoDBProperty]
        public string Description { get; set; } = string.Empty;

        [DynamoDBProperty]
        public string Caption { get; set; } = string.Empty;

        [DynamoDBProperty]
        public List<string> Tags { get; set; } = new();

        [DynamoDBProperty]
        public string Visibility { get; set; } = "public"; // "public" or "private"

        [DynamoDBProperty]
        public string ImageUrl { get; set; } = string.Empty;

        [DynamoDBProperty]
        [NotMapped]
        public Dictionary<string, string> QualityUrls { get; set; } = new();
        // { "low": "...", "medium": "...", "high": "..." }

        [DynamoDBProperty]
        public int Likes { get; set; }

        [DynamoDBProperty]
        public int Downloads { get; set; }

        [DynamoDBProperty]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [DynamoDBProperty]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        [DynamoDBProperty]
        public string Photographer { get;  set; } = string.Empty;
        [DynamoDBProperty]
        public string AspectRatio { get;  set; } = string.Empty;
        [DynamoDBProperty]
        public string? Aperture { get;  set; }
        [DynamoDBProperty]
        public string? Location { get;  set; }
        [DynamoDBProperty]
        public string? Focal { get;  set; }
        [DynamoDBProperty]
        public string? Camera { get;  set; }
        [DynamoDBProperty]
        public string? ISO { get;  set; }
        [DynamoDBProperty]
        public string? CameraModel { get;  set; }
        [DynamoDBProperty]
        public bool PublishAndDistributePermission { get;  set; }
        [DynamoDBProperty]
        public bool RightsOwned { get; set; }
    }

}
