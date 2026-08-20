using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ReviewAgent.Data.Models;

public class SyncState
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    [BsonIgnoreIfNull]
    public string? Id { get; set; }

    [BsonElement("appId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string AppId { get; set; } = string.Empty;

    [BsonElement("platform")]
    public string Platform { get; set; } = string.Empty;

    [BsonElement("lastSyncedAt")]
    public DateTime LastSyncedAt { get; set; }
}
