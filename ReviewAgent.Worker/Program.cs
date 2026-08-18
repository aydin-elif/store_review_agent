using ReviewAgent.Data;
using ReviewAgent.Data.Models;
using ReviewAgent.Data.Repositories;
using ReviewAgent.Worker;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

MongoDbContext context = new("mongodb://admin:devpassword@localhost:27017", "review_agent");
AppRepository appRepository = new(context);

await SeedData.RunAsync(appRepository);
Console.WriteLine("Seed tamamlandı.");

await context.EnsureIndexesAsync();
Console.WriteLine("Index oluşturuldu.");

List<AppRegistration> activeApps = await appRepository.GetActiveAppsAsync();
Console.WriteLine($"Aktif uygulama sayısı: {activeApps.Count}");
foreach (AppRegistration app in activeApps)
{
    Console.WriteLine($" - {app.DisplayName} ({app.AppKey})");
}

ReviewRepository reviewRepository = new(context);

Review fakeReview = new()
{
    ExternalReviewId = "test-review-001",
    AppId = activeApps[0].Id!,
    AppKey = activeApps[0].AppKey,
    Platform = "appstore",
    Rating = 5,
    Title = "Test yorum",
    Body = "Bu bir test yorumu",
    ReviewDate = DateTime.UtcNow
};

await reviewRepository.UpsertReviewAsync(fakeReview);
await reviewRepository.UpsertReviewAsync(fakeReview);
Console.WriteLine("İki kere upsert edildi, tekrar kontrolü Compass'ta yapılacak");

builder.Services.AddHostedService<Worker>();

IHost host = builder.Build();
host.Run();
