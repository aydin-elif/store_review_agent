using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ReviewAgent.Data.Models;

public class Review
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    [BsonIgnoreIfNull]
    public string? Id { get; set; }

    [BsonElement("externalReviewId")]
    public string ExternalReviewId { get; set; } = string.Empty;

    [BsonElement("appId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string AppId { get; set; } = string.Empty;

    [BsonElement("appKey")]
    public string AppKey { get; set; } = string.Empty;

    [BsonElement("platform")]
    public string Platform { get; set; } = string.Empty;

    [BsonElement("rating")]
    public int Rating { get; set; }

    [BsonElement("title")]
    public string? Title { get; set; }

    [BsonElement("body")]
    public string? Body { get; set; }

    [BsonElement("authorName")]
    public string? AuthorName { get; set; }

    [BsonElement("reviewDate")]
    public DateTime ReviewDate { get; set; }

    [BsonElement("fetchedAt")]
    public DateTime FetchedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("analysis")]
    public ReviewAnalysis? Analysis { get; set; }

    [BsonElement("notified")]
    public NotificationStatus Notified { get; set; } = new();
}

public class ReviewAnalysis
{
    [BsonElement("sentiment")]
    public string? Sentiment { get; set; }

    [BsonElement("category")]
    public string? Category { get; set; }

    [BsonElement("priorityScore")]
    public int? PriorityScore { get; set; }

    [BsonElement("summary")]
    public string? Summary { get; set; }

    [BsonElement("analyzedAt")]
    public DateTime? AnalyzedAt { get; set; }

    [BsonElement("modelVersion")]
    public string? ModelVersion { get; set; }
}

public class NotificationStatus
{
    [BsonElement("daily")]
    public bool Daily { get; set; }

    [BsonElement("critical")]
    public bool Critical { get; set; }
}
