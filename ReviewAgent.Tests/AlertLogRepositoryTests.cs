using MongoDB.Bson;
using ReviewAgent.Data;
using ReviewAgent.Data.Repositories;

namespace ReviewAgent.Tests;

public class AlertLogRepositoryTests
{
    private const string ConnectionString = "mongodb://admin:devpassword@localhost:27017";
    private const string DatabaseName = "review_agent_tests";

    [Fact]
    public async Task WasAlertSentAsync_ReturnsFalse_WhenNoAlertRecorded()
    {
        AlertLogRepository repository = CreateRepository();
        string appId = ObjectId.GenerateNewId().ToString();

        bool wasSent = await repository.WasAlertSentAsync("nonexistent-id", "appstore", appId);

        Assert.False(wasSent);
    }

    [Fact]
    public async Task RecordAlertSentAsync_ThenWasAlertSentAsync_ReturnsTrue()
    {
        AlertLogRepository repository = CreateRepository();
        string reviewId = $"test-alert-{Guid.NewGuid()}";
        string appId = ObjectId.GenerateNewId().ToString();

        await repository.RecordAlertSentAsync(reviewId, "appstore", appId);
        bool wasSent = await repository.WasAlertSentAsync(reviewId, "appstore", appId);

        Assert.True(wasSent);
    }

    private static AlertLogRepository CreateRepository()
    {
        MongoDbContext context = new(ConnectionString, DatabaseName);
        return new AlertLogRepository(context);
    }
}
