using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ReviewAgent.Data.Models;

public class AlertLog
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    [BsonIgnoreIfNull]
    public string? Id { get; set; }

    [BsonElement("reviewExternalId")]
    public string ReviewExternalId { get; set; } = string.Empty;

    [BsonElement("platform")]
    public string Platform { get; set; } = string.Empty;

    [BsonElement("appId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string AppId { get; set; } = string.Empty;

    [BsonElement("sentAt")]
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
}
