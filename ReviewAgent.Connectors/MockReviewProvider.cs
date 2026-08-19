using System.Text.Json;

namespace ReviewAgent.Connectors;

public class MockReviewProvider : IReviewProvider
{
    private readonly string _jsonFilePath;

    public MockReviewProvider(string jsonFilePath)
    {
        _jsonFilePath = jsonFilePath;
    }

    public async Task<List<RawReview>> FetchReviewsAsync(string appIdentifier, CancellationToken ct = default)
    {
        string json = await File.ReadAllTextAsync(_jsonFilePath, ct);
        JsonSerializerOptions options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        List<RawReview>? reviews = JsonSerializer.Deserialize<List<RawReview>>(json, options);
        return reviews ?? new List<RawReview>();
    }
}
