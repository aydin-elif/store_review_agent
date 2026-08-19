using ReviewAgent.Connectors;

namespace ReviewAgent.Tests;

public class MockReviewProviderTests
{
    [Fact]
    public async Task FetchReviewsAsync_ReadsAllReviewsFromJson()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "MockData", "reviews.json");
        MockReviewProvider provider = new(path);

        List<RawReview> reviews = await provider.FetchReviewsAsync("bithero");

        Assert.Equal(10, reviews.Count);
        Assert.Contains(reviews, r => r.Rating == 1);
        Assert.Contains(reviews, r => r.Rating == 5);
    }
}
