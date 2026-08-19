using ReviewAgent.Data;
using ReviewAgent.Connectors;
using ReviewAgent.Data.Repositories;
using ReviewAgent.Slack;
using ReviewAgent.Worker;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

IHost host = builder.Build();

MongoDbContext context = new("mongodb://admin:devpassword@localhost:27017", "review_agent");
AppRepository appRepository = new(context);
ReviewRepository reviewRepository = new(context);
ISlackNotifier notifier = new ConsoleSlackNotifier();
string appStoreMockPath = Path.Combine(AppContext.BaseDirectory, "MockData", "reviews_appstore.json");
string googlePlayMockPath = Path.Combine(AppContext.BaseDirectory, "MockData", "reviews_googleplay.json");
IReviewProvider appStoreProvider = new MockReviewProvider(appStoreMockPath);
IReviewProvider googlePlayProvider = new MockReviewProvider(googlePlayMockPath);

await context.EnsureIndexesAsync();
await SeedData.RunAsync(appRepository);
await DemoFlowRunner.RunAsync(
    appRepository,
    reviewRepository,
    notifier,
    appStoreProvider,
    googlePlayProvider);

host.Run();
