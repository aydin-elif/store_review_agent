using System.Text.Json.Serialization;

namespace ReviewAgent.Slack.Models;

public class SlackMessagePayload
{
    [JsonPropertyName("channel")]
    public string Channel { get; set; } = string.Empty;

    [JsonPropertyName("blocks")]
    public List<object> Blocks { get; set; } = new();
}
