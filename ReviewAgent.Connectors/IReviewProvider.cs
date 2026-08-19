namespace ReviewAgent.Connectors;

public interface IReviewProvider
{
    Task<List<RawReview>> FetchReviewsAsync(string appIdentifier, CancellationToken ct = default);
}

public class RawReview
{
    public string ExternalReviewId { get; set; } = string.Empty;

    public int Rating { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public string AuthorName { get; set; } = string.Empty;

    public DateTime ReviewDate { get; set; }

    public string Platform { get; set; } = string.Empty;
}
