using Google.Apis.AndroidPublisher.v3.Data;

namespace ReviewAgent.Connectors.GooglePlay;

public static class GooglePlayReviewMapper
{
    public static RawReview MapToRawReview(Review review)
    {
        UserComment? latestComment = review.Comments?
            .FirstOrDefault()?.UserComment;

        return new RawReview
        {
            ExternalReviewId = review.ReviewId ?? string.Empty,
            Rating = (int)(latestComment?.StarRating ?? 0),
            Title = string.Empty,
            Body = latestComment?.Text ?? string.Empty,
            AuthorName = review.AuthorName ?? string.Empty,
            ReviewDate = latestComment?.LastModified?.Seconds is long seconds
                ? DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime
                : DateTime.UtcNow,
            Platform = "googleplay"
        };
    }
}
