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

    public async Task EnsureIndexesAsync()
    {
        IndexKeysDefinition<Review> indexKeys = Builders<Review>.IndexKeys
            .Ascending(r => r.ExternalReviewId)
            .Ascending(r => r.Platform)
            .Ascending(r => r.AppId);

        CreateIndexModel<Review> indexModel = new(
            indexKeys,
            new CreateIndexOptions { Unique = true, Name = "uniq_review_per_app_platform" });

        await Reviews.Indexes.CreateOneAsync(indexModel);
    }
}
