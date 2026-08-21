using ReviewAgent.AI.Models;

namespace ReviewAgent.AI;

public class MockSentimentAnalyzer : ISentimentAnalyzer
{
    public Task<ReviewAnalysisResult> AnalyzeAsync(string reviewTitle, string reviewBody, int rating, CancellationToken ct = default)
    {
        ReviewAnalysisResult result = rating switch
        {
            <= 2 => new ReviewAnalysisResult
            {
                Sentiment = "negative",
                Category = "bug",
                PriorityScore = 4,
                Summary = "Olumsuz deneyim (mock).",
                ModelVersion = "mock-v1"
            },
            3 => new ReviewAnalysisResult
            {
                Sentiment = "neutral",
                Category = "other",
                PriorityScore = 2,
                Summary = "Nötr değerlendirme (mock).",
                ModelVersion = "mock-v1"
            },
            _ => new ReviewAnalysisResult
            {
                Sentiment = "positive",
                Category = "other",
                PriorityScore = 1,
                Summary = "Olumlu deneyim (mock).",
                ModelVersion = "mock-v1"
            }
        };

        return Task.FromResult(result);
    }
}
