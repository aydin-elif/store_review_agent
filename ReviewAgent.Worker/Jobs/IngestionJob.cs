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
    private readonly AppRepository _appRepository;
    private readonly ReviewRepository _reviewRepository;
    private readonly SyncStateRepository _syncStateRepository;
    private readonly ISlackNotifier _notifier;
    private readonly IReviewProvider _appStoreProvider;
    private readonly IReviewProvider _googlePlayProvider;
    private readonly IReviewProvider? _liveDemoProvider;

    public IngestionJob(
        AppRepository appRepository,
        ReviewRepository reviewRepository,
        SyncStateRepository syncStateRepository,
        ISlackNotifier notifier,
        IReviewProvider appStoreProvider,
        IReviewProvider googlePlayProvider,
        IReviewProvider? liveDemoProvider = null)
    {
        _appRepository = appRepository;
        _reviewRepository = reviewRepository;
        _syncStateRepository = syncStateRepository;
        _notifier = notifier;
        _appStoreProvider = appStoreProvider;
        _googlePlayProvider = googlePlayProvider;
        _liveDemoProvider = liveDemoProvider;
    }

    public async Task RunAsync()
    {
        Console.WriteLine($"[IngestionJob] Başladı: {DateTime.UtcNow:u}");

        List<AppRegistration> activeApps = await _appRepository.GetActiveAppsAsync();

        foreach (AppRegistration app in activeApps)
        {
            await ProcessAppAsync(app);
        }

        Console.WriteLine($"[IngestionJob] Tamamlandı: {DateTime.UtcNow:u}");
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
            Console.WriteLine($"[IngestionJob] {app.DisplayName}: yeni yorum yok, atlanıyor.");
            return;
        }

        foreach (RawReview raw in allRawReviews)
        {
            await _reviewRepository.UpsertReviewAsync(MapToReview(raw, app));
        }

        await _syncStateRepository.UpdateLastSyncedAtAsync(app.Id!, "appstore", DateTime.UtcNow);
        await _syncStateRepository.UpdateLastSyncedAtAsync(app.Id!, "googleplay", DateTime.UtcNow);

        Console.WriteLine($"[IngestionJob] {app.DisplayName}: {allRawReviews.Count} YENİ yorum işlendi.");

        DailySummaryStats stats = BuildStats(app.DisplayName, allRawReviews);
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

    private static DailySummaryStats BuildStats(string appDisplayName, List<RawReview> reviews)
    {
        int positive = reviews.Count(r => r.Rating >= 4);
        int negative = reviews.Count(r => r.Rating <= 2);
        int neutral = reviews.Count - positive - negative;

        List<TopReview> topPriority = reviews
            .Where(r => r.Rating <= 2)
            .OrderBy(r => r.Rating)
            .Take(5)
            .Select(r => new TopReview
            {
                Rating = r.Rating,
                Summary = string.IsNullOrWhiteSpace(r.Title) ? Truncate(r.Body, 60) : r.Title,
                PriorityScore = r.Rating == 1 ? 5 : 3
            })
            .ToList();

        return new DailySummaryStats
        {
            AppDisplayName = appDisplayName,
            TotalReviews = reviews.Count,
            PositiveCount = positive,
            NegativeCount = negative,
            NeutralCount = neutral,
            TopCategory = "bug",
            TopPriorityReviews = topPriority
        };
    }

    private static string Truncate(string text, int maxLength) =>
        text.Length <= maxLength ? text : text[..maxLength] + "...";
}
