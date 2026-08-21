namespace ReviewAgent.AI.Models;

public class ReviewAnalysisResult
{
    public string Sentiment { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public int PriorityScore { get; set; }

    public string Summary { get; set; } = string.Empty;

    public string ModelVersion { get; set; } = string.Empty;
}
