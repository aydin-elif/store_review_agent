namespace ReviewAgent.Connectors;

public class LiveDemoReviewTemplate
{
    public string TemplateId { get; set; } = string.Empty;

    public int Rating { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public string AuthorName { get; set; } = string.Empty;

    public string Platform { get; set; } = string.Empty;
}
