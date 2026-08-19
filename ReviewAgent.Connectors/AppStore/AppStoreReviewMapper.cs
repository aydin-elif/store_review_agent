namespace ReviewAgent.Connectors.AppStore;

public static class AppStoreReviewMapper
{
    public static RawReview MapToRawReview(AppStoreReviewData data) => new()
    {
        ExternalReviewId = data.Id,
        Rating = data.Attributes.Rating,
        Title = data.Attributes.Title,
        Body = data.Attributes.Body,
        AuthorName = data.Attributes.ReviewerNickname,
        ReviewDate = data.Attributes.CreatedDate,
        Platform = "appstore"
    };
}
