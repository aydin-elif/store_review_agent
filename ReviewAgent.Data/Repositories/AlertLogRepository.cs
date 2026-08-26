using MongoDB.Driver;
using ReviewAgent.Data.Models;

namespace ReviewAgent.Data.Repositories;

public class AlertLogRepository
{
    private readonly IMongoCollection<AlertLog> _alertLogs;

    public AlertLogRepository(MongoDbContext context)
    {
        _alertLogs = context.AlertLogs;
    }

    public async Task<bool> WasAlertSentAsync(string reviewExternalId, string platform, string appId, CancellationToken ct = default)
    {
        FilterDefinition<AlertLog> filter = Builders<AlertLog>.Filter.And(
            Builders<AlertLog>.Filter.Eq(a => a.ReviewExternalId, reviewExternalId),
            Builders<AlertLog>.Filter.Eq(a => a.Platform, platform),
            Builders<AlertLog>.Filter.Eq(a => a.AppId, appId));

        AlertLog? existing = await _alertLogs.Find(filter).FirstOrDefaultAsync(ct);
        return existing is not null;
    }

    public async Task RecordAlertSentAsync(string reviewExternalId, string platform, string appId, CancellationToken ct = default)
    {
        AlertLog alertLog = new()
        {
            ReviewExternalId = reviewExternalId,
            Platform = platform,
            AppId = appId,
            SentAt = DateTime.UtcNow
        };

        await _alertLogs.InsertOneAsync(alertLog, cancellationToken: ct);
    }

    public async Task<bool> TryRecordAlertSentAsync(string reviewExternalId, string platform, string appId, CancellationToken ct = default)
    {
        try
        {
            AlertLog alertLog = new()
            {
                ReviewExternalId = reviewExternalId,
                Platform = platform,
                AppId = appId,
                SentAt = DateTime.UtcNow
            };
            await _alertLogs.InsertOneAsync(alertLog, cancellationToken: ct);
            return true;
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            return false;
        }
    }
}
