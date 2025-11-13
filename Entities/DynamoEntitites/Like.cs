namespace DignaApi.Entities.DynamoEntitites
{
    using Amazon.DynamoDBv2.DataModel;

    [DynamoDBTable("Like")]
    public class Like
    {
        [DynamoDBHashKey]
        public string ImageId { get; set; } = string.Empty;

        [DynamoDBRangeKey]
        public string UserId { get; set; } = string.Empty;

        [DynamoDBProperty]
        public DateTime LikedAt { get; set; } = DateTime.UtcNow;
    }

}
