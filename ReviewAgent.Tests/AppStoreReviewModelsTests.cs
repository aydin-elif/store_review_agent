using System.Text.Json;
using ReviewAgent.Connectors.AppStore;

namespace ReviewAgent.Tests;

public class AppStoreReviewModelsTests
{
    [Fact]
    public void Deserialize_SampleAppleResponse_ParsesCorrectly()
    {
        string json = """
        {
          "data": [
            {
              "id": "1234",
              "attributes": {
                "rating": 5,
                "title": "Harika",
                "body": "Cok iyi calisiyor",
                "reviewerNickname": "kullanici1",
                "createdDate": "2026-08-01T10:00:00.000Z"
              }
            }
          ]
        }
        """;

        JsonSerializerOptions options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        AppStoreReviewsResponse? result = JsonSerializer.Deserialize<AppStoreReviewsResponse>(json, options);

        Assert.NotNull(result);
        Assert.Single(result!.Data);
        Assert.Equal(5, result.Data[0].Attributes.Rating);
        Assert.Equal("Harika", result.Data[0].Attributes.Title);
    }
}
