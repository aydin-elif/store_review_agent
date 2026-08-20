using MongoDB.Driver;
using ReviewAgent.Data.Models;

namespace ReviewAgent.Data.Repositories;

public class SyncStateRepository
{
    private readonly IMongoCollection<SyncState> _syncStates;

    public SyncStateRepository(MongoDbContext context)
    {
        _syncStates = context.SyncStates;
    }

    public async Task<DateTime?> GetLastSyncedAtAsync(string appId, string platform, CancellationToken ct = default)
    {
        FilterDefinition<SyncState> filter = Builders<SyncState>.Filter.And(
            Builders<SyncState>.Filter.Eq(s => s.AppId, appId),
            Builders<SyncState>.Filter.Eq(s => s.Platform, platform));

        SyncState? existing = await _syncStates.Find(filter).FirstOrDefaultAsync(ct);
        return existing?.LastSyncedAt;
    }

    public async Task UpdateLastSyncedAtAsync(string appId, string platform, DateTime syncedAt, CancellationToken ct = default)
    {
        FilterDefinition<SyncState> filter = Builders<SyncState>.Filter.And(
            Builders<SyncState>.Filter.Eq(s => s.AppId, appId),
            Builders<SyncState>.Filter.Eq(s => s.Platform, platform));

        UpdateDefinition<SyncState> update = Builders<SyncState>.Update
            .Set(s => s.LastSyncedAt, syncedAt)
            .Set(s => s.AppId, appId)
            .Set(s => s.Platform, platform);

        await _syncStates.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true }, ct);
    }
}
