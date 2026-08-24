using Hangfire;
using Hangfire.Mongo;
using Hangfire.Mongo.Migration;
using Hangfire.Mongo.Migration.Strategies;
using Microsoft.Extensions.Http;
using ReviewAgent.AI;
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
    builder.Services.AddSingleton<AlertLogRepository>();
    string? slackBotToken = builder.Configuration["Slack:BotToken"];
    builder.Services.AddHttpClient<SlackApiNotifier>();
    builder.Services.AddSingleton<ISlackNotifier>(sp =>
    {
        if (string.IsNullOrWhiteSpace(slackBotToken))
        {
            Log.Warning("Slack bot token bulunamadı, ConsoleSlackNotifier kullanılıyor.");
            return new ConsoleSlackNotifier();
        }

        HttpClient httpClient = sp.GetRequiredService<IHttpClientFactory>()
            .CreateClient(nameof(SlackApiNotifier));
        return new SlackApiNotifier(httpClient, slackBotToken);
    });

    string? anthropicApiKey = builder.Configuration["Anthropic:ApiKey"];
    builder.Services.AddHttpClient<AnthropicSentimentAnalyzer>();
    builder.Services.AddSingleton<ISentimentAnalyzer>(sp =>
    {
        if (string.IsNullOrWhiteSpace(anthropicApiKey))
        {
            Log.Warning("Anthropic API key bulunamadı, MockSentimentAnalyzer kullanılıyor.");
            return new MockSentimentAnalyzer();
        }

        HttpClient httpClient = sp.GetRequiredService<IHttpClientFactory>()
            .CreateClient(nameof(AnthropicSentimentAnalyzer));
        ILogger<AnthropicSentimentAnalyzer> logger = sp.GetRequiredService<ILogger<AnthropicSentimentAnalyzer>>();
        return new AnthropicSentimentAnalyzer(httpClient, anthropicApiKey, logger);
    });

    builder.Services.AddSingleton<IngestionJob>(sp =>
    {
        ILogger<IngestionJob> logger = sp.GetRequiredService<ILogger<IngestionJob>>();
        AppRepository appRepo = sp.GetRequiredService<AppRepository>();
        ReviewRepository reviewRepo = sp.GetRequiredService<ReviewRepository>();
        SyncStateRepository syncStateRepo = sp.GetRequiredService<SyncStateRepository>();
        AlertLogRepository alertLogRepo = sp.GetRequiredService<AlertLogRepository>();
        ISlackNotifier notifier = sp.GetRequiredService<ISlackNotifier>();
        ISentimentAnalyzer sentimentAnalyzer = sp.GetRequiredService<ISentimentAnalyzer>();

        MockReviewProvider appStoreProvider = new(
            Path.Combine(AppContext.BaseDirectory, "MockData", "reviews_appstore.json"));
        MockReviewProvider googlePlayProvider = new(
            Path.Combine(AppContext.BaseDirectory, "MockData", "reviews_googleplay.json"));

        // Canlı demo kapalı. Açmak için aşağıdaki satırları aktif et.
        // LiveDemoReviewProvider liveDemoProvider = new(
        //     Path.Combine(AppContext.BaseDirectory, "MockData", "reviews_live_demo.json"));

        return new IngestionJob(
            logger,
            appRepo,
            reviewRepo,
            syncStateRepo,
            alertLogRepo,
            notifier,
            sentimentAnalyzer,
            appStoreProvider,
            googlePlayProvider,
            liveDemoProvider: null);
    });

    builder.Services.AddSingleton<BackfillAnalysisJob>();

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
        "*/5 * * * *");

    // GEÇİCİ: Backfill'i bir kerelik çalıştırmak için satırı aç, çalıştırdıktan sonra tekrar kapat.
    // BackfillAnalysisJob backfillJob = app.Services.GetRequiredService<BackfillAnalysisJob>();
    // await backfillJob.RunAsync();

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
