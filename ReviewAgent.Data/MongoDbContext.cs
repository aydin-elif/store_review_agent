using MongoDB.Driver;
using ReviewAgent.Data.Models;

namespace ReviewAgent.Data;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(string connectionString, string databaseName)
    {
        MongoClient client = new(connectionString);
        _database = client.GetDatabase(databaseName);
    }

    public IMongoCollection<AppRegistration> Apps =>
        _database.GetCollection<AppRegistration>("apps");

    public IMongoCollection<Review> Reviews =>
        _database.GetCollection<Review>("reviews");

    public IMongoCollection<SyncState> SyncStates =>
        _database.GetCollection<SyncState>("sync_state");

    public IMongoCollection<AlertLog> AlertLogs =>
        _database.GetCollection<AlertLog>("alert_log");

    public async Task EnsureIndexesAsync()
    {
        IndexKeysDefinition<Review> reviewIndexKeys = Builders<Review>.IndexKeys
            .Ascending(r => r.ExternalReviewId)
            .Ascending(r => r.Platform)
            .Ascending(r => r.AppId);

        CreateIndexModel<Review> reviewIndexModel = new(
            reviewIndexKeys,
            new CreateIndexOptions { Unique = true, Name = "uniq_review_per_app_platform" });

        await Reviews.Indexes.CreateOneAsync(reviewIndexModel);

        IndexKeysDefinition<SyncState> syncStateIndexKeys = Builders<SyncState>.IndexKeys
            .Ascending(s => s.AppId)
            .Ascending(s => s.Platform);

        CreateIndexModel<SyncState> syncStateIndexModel = new(
            syncStateIndexKeys,
            new CreateIndexOptions { Unique = true, Name = "uniq_sync_state_per_app_platform" });

        await SyncStates.Indexes.CreateOneAsync(syncStateIndexModel);

        IndexKeysDefinition<AlertLog> alertLogIndexKeys = Builders<AlertLog>.IndexKeys
            .Ascending(a => a.ReviewExternalId)
            .Ascending(a => a.Platform)
            .Ascending(a => a.AppId);

        CreateIndexModel<AlertLog> alertLogIndexModel = new(
            alertLogIndexKeys,
            new CreateIndexOptions { Unique = true, Name = "uniq_alert_per_review" });

        await AlertLogs.Indexes.CreateOneAsync(alertLogIndexModel);
    }
}
