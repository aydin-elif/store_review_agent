using ReviewAgent.Data.Models;
using ReviewAgent.Slack.Models;
using ReviewAgent.Worker.Jobs;

namespace ReviewAgent.Tests;

public class IngestionStatsCalculatorFromReviewsTests
{
    private static Review MakeReview(int rating, string sentiment, string category, int priority, string summary) => new()
    {
        Rating = rating,
        Analysis = new ReviewAnalysis
        {
            Sentiment = sentiment,
            Category = category,
            PriorityScore = priority,
            Summary = summary
        }
    };

    [Fact]
    public void BuildStatsFromReviews_IgnoresUnanalyzedReviews()
    {
        List<Review> reviews =
        [
            MakeReview(1, "negative", "bug", 5, "Çöküyor"),
            new() { Rating = 3, Analysis = null }
        ];

        DailySummaryStats stats = IngestionStatsCalculator.BuildStatsFromReviews("Test App", reviews);

        Assert.Equal(1, stats.TotalReviews);
    }

    [Fact]
    public void BuildStatsFromReviews_AggregatesCorrectly()
    {
        List<Review> reviews =
        [
            MakeReview(1, "negative", "bug", 5, "Çöküyor"),
            MakeReview(5, "positive", "other", 1, "Harika")
        ];

        DailySummaryStats stats = IngestionStatsCalculator.BuildStatsFromReviews("Test App", reviews);

        Assert.Equal(2, stats.TotalReviews);
        Assert.Equal(1, stats.PositiveCount);
        Assert.Equal(1, stats.NegativeCount);
    }
}
