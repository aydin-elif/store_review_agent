using ReviewAgent.Slack;
using ReviewAgent.Slack.Models;

namespace ReviewAgent.Tests;

public class DailySummaryMessageBuilderTests
{
    [Fact]
    public void Build_WithTopReviews_IncludesHeaderAndReviewBlocks()
    {
        DailySummaryStats stats = new()
        {
            AppDisplayName = "Bithero (Test)",
            TotalReviews = 10,
            PositiveCount = 6,
            NegativeCount = 3,
            NeutralCount = 1,
            TopCategory = "bug",
            TopPriorityReviews =
            [
                new() { Rating = 1, Summary = "Uygulama çöküyor", PriorityScore = 5 }
            ]
        };

        SlackMessagePayload payload = DailySummaryMessageBuilder.Build("#store-reviews-test", stats);

        Assert.Equal("#store-reviews-test", payload.Channel);
        Assert.True(payload.Blocks.Count >= 4);
    }
}
