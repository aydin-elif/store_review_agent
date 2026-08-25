using ReviewAgent.Connectors;

namespace ReviewAgent.Worker.Jobs;

public static class ReviewBatchLimiter
{
    public static (List<RawReview> ToProcess, bool WasLimited) Limit(List<RawReview> reviews, int maxCount)
    {
        List<RawReview> sorted = reviews.OrderBy(r => r.ReviewDate).ToList();
        bool wasLimited = sorted.Count > maxCount;
        List<RawReview> toProcess = sorted.Take(maxCount).ToList();
        return (toProcess, wasLimited);
    }
}
