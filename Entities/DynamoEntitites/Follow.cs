namespace DignaApi.Entities.DynamoEntitites
{
    using Amazon.DynamoDBv2.DataModel;

    [DynamoDBTable("Follow")]
    public class Follow
    {
        [DynamoDBHashKey]
        public string FollowerId { get; set; } = string.Empty; // user who follows

        [DynamoDBRangeKey]
        public string FolloweeId { get; set; } = string.Empty; // user being followed

        [DynamoDBProperty]
        public DateTime FollowedAt { get; set; } = DateTime.UtcNow;
    }

}
