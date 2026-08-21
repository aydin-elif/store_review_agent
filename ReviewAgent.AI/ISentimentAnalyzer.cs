using ReviewAgent.AI.Models;

namespace ReviewAgent.AI;

public interface ISentimentAnalyzer
{
    Task<ReviewAnalysisResult> AnalyzeAsync(string reviewTitle, string reviewBody, int rating, CancellationToken ct = default);
}
