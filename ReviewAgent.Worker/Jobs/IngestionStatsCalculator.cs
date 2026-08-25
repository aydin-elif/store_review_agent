using ReviewAgent.AI.Models;
using ReviewAgent.Connectors;
using ReviewAgent.Slack.Models;

namespace ReviewAgent.Worker.Jobs;

public static class IngestionStatsCalculator
{
    public static DailySummaryStats BuildStats(
        string appDisplayName,
        List<(RawReview Raw, ReviewAnalysisResult Analysis)> analyzedReviews)
    {
        int positive = analyzedReviews.Count(x => x.Analysis.Sentiment == "positive");
        int negative = analyzedReviews.Count(x => x.Analysis.Sentiment == "negative");
        int neutral = analyzedReviews.Count - positive - negative;

        string topCategory = analyzedReviews
            .GroupBy(x => x.Analysis.Category)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .FirstOrDefault() ?? "other";

        List<TopReview> topPriority = analyzedReviews
            .OrderByDescending(x => x.Analysis.PriorityScore)
            .Take(5)
            .Select(x => new TopReview
            {
                Rating = x.Raw.Rating,
                Summary = x.Analysis.Summary,
                PriorityScore = x.Analysis.PriorityScore
            })
            .ToList();

        return new DailySummaryStats
        {
            AppDisplayName = appDisplayName,
            TotalReviews = analyzedReviews.Count,
            PositiveCount = positive,
            NegativeCount = negative,
            NeutralCount = neutral,
            TopCategory = topCategory,
            TopPriorityReviews = topPriority
        };
    }
}
