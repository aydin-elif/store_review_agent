using ReviewAgent.Connectors;

namespace ReviewAgent.Tests;

public class LiveDemoReviewProviderTests
{
    [Fact]
    public async Task FetchReviewsAsync_GeneratesFreshTimestampsAndUniqueIds()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "MockData", "reviews_live_demo.json");
        LiveDemoReviewProvider provider = new(path);

        List<RawReview> firstCall = await provider.FetchReviewsAsync("bithero");
        await Task.Delay(1100);
        List<RawReview> secondCall = await provider.FetchReviewsAsync("bithero");

        Assert.Equal(3, firstCall.Count);
        Assert.All(firstCall, r => Assert.True((DateTime.UtcNow - r.ReviewDate).TotalSeconds < 5));
        Assert.NotEqual(firstCall[0].ExternalReviewId, secondCall[0].ExternalReviewId);
    }
}
