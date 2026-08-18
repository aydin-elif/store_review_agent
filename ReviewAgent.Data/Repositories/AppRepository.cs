using MongoDB.Driver;
using ReviewAgent.Data.Models;

namespace ReviewAgent.Data.Repositories;

public class AppRepository
{
    private readonly IMongoCollection<AppRegistration> _apps;

    public AppRepository(MongoDbContext context)
    {
        _apps = context.Apps;
    }

    public async Task<List<AppRegistration>> GetActiveAppsAsync(CancellationToken ct = default)
    {
        return await _apps.Find(a => a.IsActive).ToListAsync(ct);
    }

    public async Task UpsertAppAsync(AppRegistration app, CancellationToken ct = default)
    {
        FilterDefinition<AppRegistration> filter = Builders<AppRegistration>.Filter.Eq(a => a.AppKey, app.AppKey);
        AppRegistration? existing = await _apps.Find(filter).FirstOrDefaultAsync(ct);
        if (existing is not null)
        {
            app.Id = existing.Id;
        }

        await _apps.ReplaceOneAsync(filter, app, new ReplaceOptions { IsUpsert = true }, ct);
    }
}
