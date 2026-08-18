using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ReviewAgent.Data.Models;

public class AppRegistration
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    [BsonIgnoreIfNull]
    public string? Id { get; set; }

    [BsonElement("appKey")]
    public string AppKey { get; set; } = string.Empty;

    [BsonElement("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [BsonElement("appStore")]
    public AppStoreConfig? AppStore { get; set; }

    [BsonElement("googlePlay")]
    public GooglePlayConfig? GooglePlay { get; set; }

    [BsonElement("slackChannel")]
    public string? SlackChannel { get; set; }

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class AppStoreConfig
{
    [BsonElement("bundleId")]
    public string BundleId { get; set; } = string.Empty;

    [BsonElement("appId")]
    public string AppId { get; set; } = string.Empty;

    [BsonElement("keyId")]
    public string? KeyId { get; set; }

    [BsonElement("issuerId")]
    public string? IssuerId { get; set; }

    [BsonElement("privateKeySecretRef")]
    public string? PrivateKeySecretRef { get; set; }
}

public class GooglePlayConfig
{
    [BsonElement("packageName")]
    public string PackageName { get; set; } = string.Empty;

    [BsonElement("serviceAccountSecretRef")]
    public string? ServiceAccountSecretRef { get; set; }
}
