using ReviewAgent.Connectors;
using ReviewAgent.Data;
using ReviewAgent.Data.Models;
using ReviewAgent.Data.Repositories;
using ReviewAgent.Slack;
using ReviewAgent.Slack.Models;

namespace ReviewAgent.Worker;

/// <summary>
/// GEÇİCİ: Gerçek credential'lar gelmeden önce, tüm katmanların birbirine
/// doğru bağlandığını doğrulamak için mock veri setiyle uçtan uca akış.
/// Not: AI katmanı henüz bağlanmadı, kategori/öncelik skoru burada
/// rating'e dayalı KABA bir yaklaşımla hesaplanıyor — gerçek analiz değil.
/// Bithero credential'ları geldiğinde bu sınıf silinip yerine
/// gerçek IngestionJob (Hangfire ile zamanlanmış) gelecek.
/// </summary>
public static class DemoFlowRunner
{
    public static async Task RunAsync(
        AppRepository appRepository,
        ReviewRepository reviewRepository,
        ISlackNotifier notifier,
        IReviewProvider appStoreProvider,
        IReviewProvider googlePlayProvider)
    {
        Console.WriteLine("=== Demo akışı başlıyor (mock veri seti) ===");

        List<AppRegistration> activeApps = await appRepository.GetActiveAppsAsync();
        AppRegistration? app = activeApps.FirstOrDefault();
        if (app is null)
        {
            Console.WriteLine("Aktif uygulama bulunamadı, demo durduruldu.");
            return;
        }

        List<RawReview> appStoreReviews = await appStoreProvider.FetchReviewsAsync(app.AppStore?.AppId ?? app.AppKey);
        List<RawReview> googlePlayReviews = await googlePlayProvider.FetchReviewsAsync(app.GooglePlay?.PackageName ?? app.AppKey);
        List<RawReview> allRawReviews = appStoreReviews.Concat(googlePlayReviews).ToList();

        Console.WriteLine($"{allRawReviews.Count} yorum çekildi ({appStoreReviews.Count} App Store, {googlePlayReviews.Count} Google Play).");

        foreach (RawReview raw in allRawReviews)
        {
            Review review = MapToReview(raw, app);
            await reviewRepository.UpsertReviewAsync(review);
        }

        Console.WriteLine("Tüm yorumlar kaydedildi (idempotent upsert).");

        DailySummaryStats stats = BuildStats(app.DisplayName, allRawReviews);

        SlackMessagePayload payload = DailySummaryMessageBuilder.Build(app.SlackChannel ?? "#store-reviews-test", stats);
        await notifier.SendAsync(payload);

        Console.WriteLine("=== Demo akışı tamamlandı ===");
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
