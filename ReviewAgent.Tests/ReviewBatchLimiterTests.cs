using ReviewAgent.Connectors;
using ReviewAgent.Worker.Jobs;

namespace ReviewAgent.Tests;

public class ReviewBatchLimiterTests
{
    [Fact]
    public void Limit_WhenUnderLimit_ReturnsAllReviews()
    {
        List<RawReview> reviews = Enumerable.Range(1, 10)
            .Select(i => new RawReview { ReviewDate = DateTime.UtcNow.AddMinutes(-i) })
            .ToList();

        (List<RawReview> toProcess, bool wasLimited) = ReviewBatchLimiter.Limit(reviews, 50);

        Assert.Equal(10, toProcess.Count);
        Assert.False(wasLimited);
    }

    [Fact]
    public void Limit_WhenOverLimit_ReturnsOldestFirst()
    {
        List<RawReview> reviews = Enumerable.Range(1, 100)
            .Select(i => new RawReview { ReviewDate = DateTime.UtcNow.AddMinutes(-i), ExternalReviewId = $"r{i}" })
            .ToList();

        (List<RawReview> toProcess, bool wasLimited) = ReviewBatchLimiter.Limit(reviews, 50);

        Assert.Equal(50, toProcess.Count);
        Assert.True(wasLimited);
        Assert.Equal("r100", toProcess[0].ExternalReviewId);
    }
}
