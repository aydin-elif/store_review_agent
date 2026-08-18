using ReviewAgent.Data;
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

await context.EnsureIndexesAsync();
await SeedData.RunAsync(appRepository);
await DemoFlowRunner.RunAsync(appRepository, reviewRepository, notifier);

host.Run();
