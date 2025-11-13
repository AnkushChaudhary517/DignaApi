namespace DignaApi.Entities.DynamoEntitites
{
    using Amazon.DynamoDBv2.DataModel;

    [DynamoDBTable("TagIndex")]
    public class TagIndex
    {
        [DynamoDBHashKey]
        public string Tag { get; set; } = string.Empty;

        [DynamoDBRangeKey]
        public string ImageId { get; set; } = string.Empty;

        [DynamoDBProperty]
        public string UserId { get; set; } = string.Empty;

        [DynamoDBProperty]
        public string Visibility { get; set; } = "public";

        [DynamoDBProperty]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

}
