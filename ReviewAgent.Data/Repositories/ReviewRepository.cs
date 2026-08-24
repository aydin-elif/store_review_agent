using MongoDB.Driver;
using ReviewAgent.Data.Models;

namespace ReviewAgent.Data.Repositories;

public class ReviewRepository
{
    private readonly IMongoCollection<Review> _reviews;

    public ReviewRepository(MongoDbContext context)
    {
        _reviews = context.Reviews;
    }

    public async Task UpsertReviewAsync(Review review, CancellationToken ct = default)
    {
        FilterDefinition<Review> filter = Builders<Review>.Filter.And(
            Builders<Review>.Filter.Eq(r => r.ExternalReviewId, review.ExternalReviewId),
            Builders<Review>.Filter.Eq(r => r.Platform, review.Platform),
            Builders<Review>.Filter.Eq(r => r.AppId, review.AppId));

        Review? existing = await _reviews.Find(filter).FirstOrDefaultAsync(ct);
        if (existing is not null)
        {
            review.Id = existing.Id;
        }

        await _reviews.ReplaceOneAsync(filter, review, new ReplaceOptions { IsUpsert = true }, ct);
    }

    public async Task<List<Review>> GetUnanalyzedReviewsAsync(CancellationToken ct = default)
    {
        FilterDefinition<Review> filter = Builders<Review>.Filter.Eq(r => r.Analysis, null);
        return await _reviews.Find(filter).ToListAsync(ct);
    }

    public async Task UpdateAnalysisAsync(string reviewId, ReviewAnalysis analysis, CancellationToken ct = default)
    {
        FilterDefinition<Review> filter = Builders<Review>.Filter.Eq(r => r.Id, reviewId);
        UpdateDefinition<Review> update = Builders<Review>.Update.Set(r => r.Analysis, analysis);
        await _reviews.UpdateOneAsync(filter, update, cancellationToken: ct);
    }
}
