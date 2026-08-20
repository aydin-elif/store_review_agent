using Hangfire;
using Hangfire.Mongo;
using Hangfire.Mongo.Migration;
using Hangfire.Mongo.Migration.Strategies;
using ReviewAgent.Connectors;
using ReviewAgent.Data;
using ReviewAgent.Data.Repositories;
using ReviewAgent.Slack;
using ReviewAgent.Worker.Jobs;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/log-.txt",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("Uygulama başlatılıyor...");

    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    const string connectionString = "mongodb://admin:devpassword@localhost:27017";
    const string databaseName = "review_agent";

    builder.Services.AddSingleton(new MongoDbContext(connectionString, databaseName));
    builder.Services.AddSingleton<AppRepository>();
    builder.Services.AddSingleton<ReviewRepository>();
    builder.Services.AddSingleton<SyncStateRepository>();
    builder.Services.AddSingleton<ISlackNotifier, ConsoleSlackNotifier>();

    builder.Services.AddSingleton<IngestionJob>(sp =>
    {
        ILogger<IngestionJob> logger = sp.GetRequiredService<ILogger<IngestionJob>>();
        AppRepository appRepo = sp.GetRequiredService<AppRepository>();
        ReviewRepository reviewRepo = sp.GetRequiredService<ReviewRepository>();
        SyncStateRepository syncStateRepo = sp.GetRequiredService<SyncStateRepository>();
        ISlackNotifier notifier = sp.GetRequiredService<ISlackNotifier>();

        MockReviewProvider appStoreProvider = new(
            Path.Combine(AppContext.BaseDirectory, "MockData", "reviews_appstore.json"));
        MockReviewProvider googlePlayProvider = new(
            Path.Combine(AppContext.BaseDirectory, "MockData", "reviews_googleplay.json"));
        LiveDemoReviewProvider liveDemoProvider = new(
            Path.Combine(AppContext.BaseDirectory, "MockData", "reviews_live_demo.json"));

        return new IngestionJob(
            logger,
            appRepo,
            reviewRepo,
            syncStateRepo,
            notifier,
            appStoreProvider,
            googlePlayProvider,
            liveDemoProvider);
    });

    builder.Services.AddHangfire(config => config
        .UseMongoStorage(connectionString, "review_agent_hangfire", new MongoStorageOptions
        {
            MigrationOptions = new MongoMigrationOptions
            {
                MigrationStrategy = new MigrateMongoMigrationStrategy()
            }
        }));
    builder.Services.AddHangfireServer();

    WebApplication app = builder.Build();

    MongoDbContext context = app.Services.GetRequiredService<MongoDbContext>();
    AppRepository appRepository = app.Services.GetRequiredService<AppRepository>();
    await context.EnsureIndexesAsync();
    await SeedData.RunAsync(appRepository);

    IRecurringJobManager recurringJobManager = app.Services.GetRequiredService<IRecurringJobManager>();
    recurringJobManager.AddOrUpdate<IngestionJob>(
        "ingestion-job",
        job => job.RunAsync(),
        "*/1 * * * *");

    app.UseHangfireDashboard("/hangfire");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Uygulama beklenmedik şekilde durdu");
}
finally
{
    Log.CloseAndFlush();
}
