using System.Text.Json;

namespace ReviewAgent.Connectors;

/// <summary>
/// SADECE DEMO/SUNUM amaçlı. Her çağrıldığında şablon dosyasındaki yorumları
/// GÜNCEL zaman damgası ve BENZERSİZ ID ile üretir, böylece sync_state filtresine
/// takılmaz — her Hangfire döngüsünde "yeni gelmiş" gibi görünür.
/// Gerçek credential'lar geldiğinde bu provider ingestion akışından çıkarılacak.
/// </summary>
public class LiveDemoReviewProvider : IReviewProvider
{
    private readonly string _templateFilePath;

    public LiveDemoReviewProvider(string templateFilePath)
    {
        _templateFilePath = templateFilePath;
    }

    public async Task<List<RawReview>> FetchReviewsAsync(string appIdentifier, CancellationToken ct = default)
    {
        string json = await File.ReadAllTextAsync(_templateFilePath, ct);
        JsonSerializerOptions options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        List<LiveDemoReviewTemplate> templates =
            JsonSerializer.Deserialize<List<LiveDemoReviewTemplate>>(json, options) ?? [];

        DateTime now = DateTime.UtcNow;
        string runSuffix = now.ToString("yyyyMMddHHmmss");

        return templates.Select(t => new RawReview
        {
            ExternalReviewId = $"{t.TemplateId}-{runSuffix}",
            Rating = t.Rating,
            Title = t.Title,
            Body = t.Body,
            AuthorName = t.AuthorName,
            ReviewDate = now,
            Platform = t.Platform
        }).ToList();
    }
}
