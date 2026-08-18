namespace ReviewAgent.Slack.Models;

public class DailySummaryStats
{
    public string AppDisplayName { get; set; } = string.Empty;

    public int TotalReviews { get; set; }

    public int PositiveCount { get; set; }

    public int NegativeCount { get; set; }

    public int NeutralCount { get; set; }

    public string TopCategory { get; set; } = string.Empty;

    public List<TopReview> TopPriorityReviews { get; set; } = new();
}

public class TopReview
{
    public int Rating { get; set; }

    public string Summary { get; set; } = string.Empty;

    public int PriorityScore { get; set; }
}
