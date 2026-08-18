using ReviewAgent.Data.Models;
using ReviewAgent.Data.Repositories;
using ReviewAgent.Slack;
using ReviewAgent.Slack.Models;

namespace ReviewAgent.Worker;

/// <summary>
/// GEÇİCİ: Gerçek credential'lar gelmeden önce, tüm katmanların birbirine
/// doğru bağlandığını doğrulamak için uçtan uca sahte akış.
/// Bithero credential'ları geldiğinde bu sınıf silinip yerine
/// gerçek IngestionJob (Hangfire ile zamanlanmış) gelecek.
/// </summary>
public static class DemoFlowRunner
{
    public static async Task RunAsync(
        AppRepository appRepository,
        ReviewRepository reviewRepository,
        ISlackNotifier notifier)
    {
        Console.WriteLine("=== Demo akışı başlıyor ===");

        List<AppRegistration> activeApps = await appRepository.GetActiveAppsAsync();
        AppRegistration? app = activeApps.FirstOrDefault();
        if (app is null)
        {
            Console.WriteLine("Aktif uygulama bulunamadı, demo durduruldu.");
            return;
        }

        Review fakeReview = new()
        {
            ExternalReviewId = "demo-review-" + Guid.NewGuid().ToString("N")[..8],
            AppId = app.Id!,
            AppKey = app.AppKey,
            Platform = "appstore",
            Rating = 2,
            Title = "Giriş yapamıyorum",
            Body = "Uygulama açılışta donuyor, giriş ekranına gelemiyorum.",
            ReviewDate = DateTime.UtcNow
        };

        await reviewRepository.UpsertReviewAsync(fakeReview);
        Console.WriteLine($"Yorum kaydedildi: {fakeReview.ExternalReviewId}");

        DailySummaryStats stats = new()
        {
            AppDisplayName = app.DisplayName,
            TotalReviews = 1,
            PositiveCount = 0,
            NegativeCount = 1,
            NeutralCount = 0,
            TopCategory = "bug",
            TopPriorityReviews =
            [
                new TopReview { Rating = fakeReview.Rating, Summary = fakeReview.Title ?? string.Empty, PriorityScore = 4 }
            ]
        };

        SlackMessagePayload payload = DailySummaryMessageBuilder.Build(app.SlackChannel ?? "#store-reviews-test", stats);
        await notifier.SendAsync(payload);

        Console.WriteLine("=== Demo akışı tamamlandı ===");
    }
}
