using ReviewAgent.AI.Models;
using ReviewAgent.Connectors;
using ReviewAgent.Slack.Models;
using ReviewAgent.Worker.Jobs;

namespace ReviewAgent.Tests;

public class IngestionStatsCalculatorTests
{
    private static (RawReview Raw, ReviewAnalysisResult Analysis) Make(
        int rating,
        string sentiment,
        string category,
        int priority,
        string summary)
    {
        RawReview raw = new() { Rating = rating, Platform = "appstore" };
        ReviewAnalysisResult analysis = new()
        {
            Sentiment = sentiment,
            Category = category,
            PriorityScore = priority,
            Summary = summary,
            ModelVersion = "test"
        };
        return (raw, analysis);
    }

    [Fact]
    public void BuildStats_CountsSentimentsCorrectly()
    {
        List<(RawReview Raw, ReviewAnalysisResult Analysis)> reviews =
        [
            Make(1, "negative", "bug", 5, "Çöküyor"),
            Make(5, "positive", "other", 1, "Harika"),
            Make(3, "neutral", "ux", 2, "Fena değil")
        ];

        DailySummaryStats stats = IngestionStatsCalculator.BuildStats("Test App", reviews);

        Assert.Equal(3, stats.TotalReviews);
        Assert.Equal(1, stats.PositiveCount);
        Assert.Equal(1, stats.NegativeCount);
        Assert.Equal(1, stats.NeutralCount);
    }

    [Fact]
    public void BuildStats_PicksMostFrequentCategory()
    {
        List<(RawReview Raw, ReviewAnalysisResult Analysis)> reviews =
        [
            Make(1, "negative", "bug", 5, "Çöküyor 1"),
            Make(2, "negative", "bug", 4, "Çöküyor 2"),
            Make(3, "neutral", "ux", 2, "UX sorunu")
        ];

        DailySummaryStats stats = IngestionStatsCalculator.BuildStats("Test App", reviews);

        Assert.Equal("bug", stats.TopCategory);
    }

    [Fact]
    public void BuildStats_OrdersTopPriorityReviewsDescending()
    {
        List<(RawReview Raw, ReviewAnalysisResult Analysis)> reviews =
        [
            Make(3, "neutral", "other", 2, "Orta öncelik"),
            Make(1, "negative", "bug", 5, "En yüksek öncelik"),
            Make(2, "negative", "performance", 3, "Düşük-orta öncelik")
        ];

        DailySummaryStats stats = IngestionStatsCalculator.BuildStats("Test App", reviews);

        Assert.Equal("En yüksek öncelik", stats.TopPriorityReviews[0].Summary);
        Assert.Equal(5, stats.TopPriorityReviews[0].PriorityScore);
    }

    [Fact]
    public void BuildStats_LimitsTopPriorityToFive()
    {
        List<(RawReview Raw, ReviewAnalysisResult Analysis)> reviews = Enumerable.Range(1, 8)
            .Select(i => Make(1, "negative", "bug", i, $"Yorum {i}"))
            .ToList();

        DailySummaryStats stats = IngestionStatsCalculator.BuildStats("Test App", reviews);

        Assert.Equal(5, stats.TopPriorityReviews.Count);
    }

    [Fact]
    public void BuildStats_EmptyList_ReturnsZeroedStats()
    {
        DailySummaryStats stats = IngestionStatsCalculator.BuildStats(
            "Test App",
            new List<(RawReview Raw, ReviewAnalysisResult Analysis)>());

        Assert.Equal(0, stats.TotalReviews);
        Assert.Equal("other", stats.TopCategory);
        Assert.Empty(stats.TopPriorityReviews);
    }
}
