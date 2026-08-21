using ReviewAgent.AI;
using ReviewAgent.AI.Models;
using ReviewAgent.Connectors;
using ReviewAgent.Data.Models;
using ReviewAgent.Data.Repositories;
using ReviewAgent.Slack;
using ReviewAgent.Slack.Models;

namespace ReviewAgent.Worker.Jobs;

/// <summary>
/// Hangfire tarafından periyodik olarak tetiklenen ana ingestion job'ı.
/// Şu an mock provider'lar kullanıyor; gerçek credential'lar geldiğinde
/// yalnızca hangi IReviewProvider implementasyonunun enjekte edildiği değişecek.
/// </summary>
public class IngestionJob
{
    private readonly ILogger<IngestionJob> _logger;
    private readonly AppRepository _appRepository;
    private readonly ReviewRepository _reviewRepository;
    private readonly SyncStateRepository _syncStateRepository;
    private readonly ISlackNotifier _notifier;
    private readonly ISentimentAnalyzer _sentimentAnalyzer;
    private readonly IReviewProvider _appStoreProvider;
    private readonly IReviewProvider _googlePlayProvider;
    private readonly IReviewProvider? _liveDemoProvider;

    public IngestionJob(
        ILogger<IngestionJob> logger,
        AppRepository appRepository,
        ReviewRepository reviewRepository,
        SyncStateRepository syncStateRepository,
        ISlackNotifier notifier,
        ISentimentAnalyzer sentimentAnalyzer,
        IReviewProvider appStoreProvider,
        IReviewProvider googlePlayProvider,
        IReviewProvider? liveDemoProvider = null)
    {
        _logger = logger;
        _appRepository = appRepository;
        _reviewRepository = reviewRepository;
        _syncStateRepository = syncStateRepository;
        _notifier = notifier;
        _sentimentAnalyzer = sentimentAnalyzer;
        _appStoreProvider = appStoreProvider;
        _googlePlayProvider = googlePlayProvider;
        _liveDemoProvider = liveDemoProvider;
    }

    public async Task RunAsync()
    {
        _logger.LogInformation("Ingestion job başladı");

        List<AppRegistration> activeApps = await _appRepository.GetActiveAppsAsync();

        foreach (AppRegistration app in activeApps)
        {
            await ProcessAppAsync(app);
        }

        _logger.LogInformation("Ingestion job tamamlandı");
    }

    private async Task ProcessAppAsync(AppRegistration app)
    {
        DateTime lastSyncAppStore = await _syncStateRepository.GetLastSyncedAtAsync(app.Id!, "appstore") ?? DateTime.MinValue;
        DateTime lastSyncGooglePlay = await _syncStateRepository.GetLastSyncedAtAsync(app.Id!, "googleplay") ?? DateTime.MinValue;

        List<RawReview> appStoreReviews = (await _appStoreProvider.FetchReviewsAsync(app.AppStore?.AppId ?? app.AppKey))
            .Where(r => r.ReviewDate > lastSyncAppStore)
            .ToList();
        List<RawReview> googlePlayReviews = (await _googlePlayProvider.FetchReviewsAsync(app.GooglePlay?.PackageName ?? app.AppKey))
            .Where(r => r.ReviewDate > lastSyncGooglePlay)
            .ToList();

        List<RawReview> allRawReviews = appStoreReviews.Concat(googlePlayReviews).ToList();

        if (_liveDemoProvider is not null)
        {
            List<RawReview> liveReviews = await _liveDemoProvider.FetchReviewsAsync(app.AppKey);
            allRawReviews.AddRange(liveReviews);
        }

        if (allRawReviews.Count == 0)
        {
            _logger.LogInformation("{AppName}: yeni yorum yok, atlanıyor", app.DisplayName);
            return;
        }

        List<(RawReview Raw, ReviewAnalysisResult Analysis)> analyzedReviews = [];

        foreach (RawReview raw in allRawReviews)
        {
            Review review = MapToReview(raw, app);
            ReviewAnalysisResult analysis = await _sentimentAnalyzer.AnalyzeAsync(raw.Title, raw.Body, raw.Rating);
            review.Analysis = new ReviewAnalysis
            {
                Sentiment = analysis.Sentiment,
                Category = analysis.Category,
                PriorityScore = analysis.PriorityScore,
                Summary = analysis.Summary,
                AnalyzedAt = DateTime.UtcNow,
                ModelVersion = analysis.ModelVersion
            };

            await _reviewRepository.UpsertReviewAsync(review);
            analyzedReviews.Add((raw, analysis));
        }

        await _syncStateRepository.UpdateLastSyncedAtAsync(app.Id!, "appstore", DateTime.UtcNow);
        await _syncStateRepository.UpdateLastSyncedAtAsync(app.Id!, "googleplay", DateTime.UtcNow);

        _logger.LogInformation("{AppName}: {Count} YENİ yorum işlendi", app.DisplayName, allRawReviews.Count);

        DailySummaryStats stats = BuildStats(app.DisplayName, analyzedReviews);
        SlackMessagePayload payload = DailySummaryMessageBuilder.Build(app.SlackChannel ?? "#store-reviews-test", stats);
        await _notifier.SendAsync(payload);
    }

    private static Review MapToReview(RawReview raw, AppRegistration app) => new()
    {
        ExternalReviewId = raw.ExternalReviewId,
        AppId = app.Id!,
        AppKey = app.AppKey,
        Platform = raw.Platform,
        Rating = raw.Rating,
        Title = raw.Title,
        Body = raw.Body,
        AuthorName = raw.AuthorName,
        ReviewDate = raw.ReviewDate
    };

    private static DailySummaryStats BuildStats(
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
