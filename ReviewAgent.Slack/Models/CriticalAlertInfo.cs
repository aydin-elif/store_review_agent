namespace ReviewAgent.Slack.Models;

public class CriticalAlertInfo
{
    public string AppDisplayName { get; set; } = string.Empty;

    public string Platform { get; set; } = string.Empty;

    public int Rating { get; set; }

    public string Summary { get; set; } = string.Empty;

    public string? ReviewUrl { get; set; }
}
