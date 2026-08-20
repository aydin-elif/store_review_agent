using MongoDB.Bson;
using ReviewAgent.Data;
using ReviewAgent.Data.Repositories;

namespace ReviewAgent.Tests;

public class SyncStateRepositoryTests
{
    private const string ConnectionString = "mongodb://admin:devpassword@localhost:27017";
    private const string DatabaseName = "review_agent_tests";

    [Fact]
    public async Task GetLastSyncedAtAsync_ReturnsNull_WhenNoStateExists()
    {
        SyncStateRepository repository = CreateRepository();
        string appId = ObjectId.GenerateNewId().ToString();

        DateTime? lastSyncedAt = await repository.GetLastSyncedAtAsync(appId, "appstore");

        Assert.Null(lastSyncedAt);
    }

    [Fact]
    public async Task UpdateLastSyncedAtAsync_CanBeReadBack()
    {
        SyncStateRepository repository = CreateRepository();
        string appId = ObjectId.GenerateNewId().ToString();
        DateTime syncedAt = new(2026, 8, 20, 12, 0, 0, DateTimeKind.Utc);

        await repository.UpdateLastSyncedAtAsync(appId, "appstore", syncedAt);
        DateTime? lastSyncedAt = await repository.GetLastSyncedAtAsync(appId, "appstore");

        Assert.NotNull(lastSyncedAt);
        Assert.Equal(syncedAt, lastSyncedAt.Value.ToUniversalTime());
    }

    [Fact]
    public async Task UpdateLastSyncedAtAsync_UpsertsSameAppAndPlatform()
    {
        SyncStateRepository repository = CreateRepository();
        string appId = ObjectId.GenerateNewId().ToString();
        DateTime firstSyncedAt = new(2026, 8, 20, 10, 0, 0, DateTimeKind.Utc);
        DateTime secondSyncedAt = new(2026, 8, 20, 11, 0, 0, DateTimeKind.Utc);

        await repository.UpdateLastSyncedAtAsync(appId, "appstore", firstSyncedAt);
        await repository.UpdateLastSyncedAtAsync(appId, "appstore", secondSyncedAt);

        DateTime? appStoreSyncedAt = await repository.GetLastSyncedAtAsync(appId, "appstore");
        DateTime? googlePlaySyncedAt = await repository.GetLastSyncedAtAsync(appId, "googleplay");

        Assert.NotNull(appStoreSyncedAt);
        Assert.Equal(secondSyncedAt, appStoreSyncedAt.Value.ToUniversalTime());
        Assert.Null(googlePlaySyncedAt);
    }

    private static SyncStateRepository CreateRepository()
    {
        MongoDbContext context = new(ConnectionString, DatabaseName);
        return new SyncStateRepository(context);
    }
}
