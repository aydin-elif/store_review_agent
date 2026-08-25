using ReviewAgent.Data.Models;
using ReviewAgent.Data.Repositories;
using ReviewAgent.Slack;
using ReviewAgent.Slack.Models;

namespace ReviewAgent.Worker.Jobs;

/// <summary>
/// Haftalık olarak tetiklenen özet job'ı. Günlük ingestion'dan bağımsızdır;
/// MongoDB'de son 7 gün içinde zaten analiz edilmiş yorumları aggregate eder.
/// </summary>
public class WeeklySummaryJob
{
    private readonly ILogger<WeeklySummaryJob> _logger;
    private readonly AppRepository _appRepository;
    private readonly ReviewRepository _reviewRepository;
    private readonly ISlackNotifier _notifier;

    public WeeklySummaryJob(
        ILogger<WeeklySummaryJob> logger,
        AppRepository appRepository,
        ReviewRepository reviewRepository,
        ISlackNotifier notifier)
    {
        _logger = logger;
        _appRepository = appRepository;
        _reviewRepository = reviewRepository;
        _notifier = notifier;
    }

    public async Task RunAsync()
    {
        try
        {
            _logger.LogInformation("Haftalık özet job'ı başladı");

            List<AppRegistration> activeApps = await _appRepository.GetActiveAppsAsync();
            DateTime since = DateTime.UtcNow.AddDays(-7);

            foreach (AppRegistration app in activeApps)
            {
                List<Review> reviews = await _reviewRepository.GetReviewsSinceAsync(app.Id!, since);

                if (reviews.Count == 0)
                {
                    _logger.LogInformation("{AppName}: son 7 günde yorum yok, haftalık özet atlanıyor", app.DisplayName);
                    continue;
                }

                DailySummaryStats stats = IngestionStatsCalculator.BuildStatsFromReviews(app.DisplayName, reviews);
                SlackMessagePayload payload = DailySummaryMessageBuilder.Build(
                    app.SlackChannel ?? "#store-reviews-test",
                    stats,
                    periodLabel: "Haftalık");
                await _notifier.SendAsync(payload);

                _logger.LogInformation("{AppName}: haftalık özet gönderildi ({Count} yorum)", app.DisplayName, reviews.Count);
            }

            _logger.LogInformation("Haftalık özet job'ı tamamlandı");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Haftalık özet job'ı başarısız oldu");
        }
    }
}
