using ReviewAgent.AI;
using ReviewAgent.AI.Models;
using ReviewAgent.Data.Models;
using ReviewAgent.Data.Repositories;

namespace ReviewAgent.Worker.Jobs;

/// <summary>
/// GEÇİCİ / TEK SEFERLİK: analysis alanı null olan (AI bağlanmadan önce
/// işlenmiş) eski yorumları geriye dönük analiz eder. Elle tetiklenir,
/// zamanlanmış bir job değildir.
/// </summary>
public class BackfillAnalysisJob
{
    private readonly ILogger<BackfillAnalysisJob> _logger;
    private readonly ReviewRepository _reviewRepository;
    private readonly ISentimentAnalyzer _sentimentAnalyzer;

    public BackfillAnalysisJob(
        ILogger<BackfillAnalysisJob> logger,
        ReviewRepository reviewRepository,
        ISentimentAnalyzer sentimentAnalyzer)
    {
        _logger = logger;
        _reviewRepository = reviewRepository;
        _sentimentAnalyzer = sentimentAnalyzer;
    }

    public async Task RunAsync()
    {
        List<Review> unanalyzed = await _reviewRepository.GetUnanalyzedReviewsAsync();

        _logger.LogInformation("Backfill: {Count} analiz edilmemiş yorum bulundu", unanalyzed.Count);

        if (unanalyzed.Count == 0)
        {
            return;
        }

        int processed = 0;

        foreach (Review review in unanalyzed)
        {
            ReviewAnalysisResult result = await _sentimentAnalyzer.AnalyzeAsync(
                review.Title ?? string.Empty,
                review.Body ?? string.Empty,
                review.Rating);

            ReviewAnalysis analysis = new()
            {
                Sentiment = result.Sentiment,
                Category = result.Category,
                PriorityScore = result.PriorityScore,
                Summary = result.Summary,
                AnalyzedAt = DateTime.UtcNow,
                ModelVersion = result.ModelVersion
            };

            await _reviewRepository.UpdateAnalysisAsync(review.Id!, analysis);
            processed++;

            _logger.LogInformation(
                "Backfill: {Processed}/{Total} tamamlandı ({ReviewId})",
                processed,
                unanalyzed.Count,
                review.ExternalReviewId);
        }

        _logger.LogInformation("Backfill tamamlandı: {Processed} yorum analiz edildi", processed);
    }
}
