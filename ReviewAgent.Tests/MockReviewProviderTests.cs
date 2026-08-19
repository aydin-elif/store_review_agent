using ReviewAgent.Connectors;

namespace ReviewAgent.Tests;

public class MockReviewProviderTests
{
    [Fact]
    public async Task FetchReviewsAsync_ReadsAllAppStoreReviews()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "MockData", "reviews_appstore.json");
        MockReviewProvider provider = new(path);

        List<RawReview> reviews = await provider.FetchReviewsAsync("bithero");

        Assert.Equal(30, reviews.Count);
        Assert.Contains(reviews, r => r.Rating == 1);
        Assert.Contains(reviews, r => r.Rating == 5);
    }

    [Fact]
    public async Task FetchReviewsAsync_ReadsAllGooglePlayReviews()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "MockData", "reviews_googleplay.json");
        MockReviewProvider provider = new(path);

        List<RawReview> reviews = await provider.FetchReviewsAsync("bithero");

        Assert.Equal(30, reviews.Count);
        Assert.Contains(reviews, r => r.Rating == 1);
        Assert.Contains(reviews, r => r.Rating == 5);
    }
}
