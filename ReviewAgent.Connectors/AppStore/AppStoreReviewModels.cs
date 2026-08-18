namespace ReviewAgent.Connectors.AppStore;

public class AppStoreReviewsResponse
{
    public List<AppStoreReviewData> Data { get; set; } = new();

    public AppStoreReviewsLinks? Links { get; set; }
}

public class AppStoreReviewData
{
    public string Id { get; set; } = string.Empty;

    public AppStoreReviewAttributes Attributes { get; set; } = new();
}

public class AppStoreReviewAttributes
{
    public int Rating { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public string ReviewerNickname { get; set; } = string.Empty;

    public DateTime CreatedDate { get; set; }
}

public class AppStoreReviewsLinks
{
    public string? Next { get; set; }
}
