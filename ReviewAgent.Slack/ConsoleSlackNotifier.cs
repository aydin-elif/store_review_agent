using System.Text.Json;
using ReviewAgent.Slack.Models;

namespace ReviewAgent.Slack;

public class ConsoleSlackNotifier : ISlackNotifier
{
    public Task SendAsync(SlackMessagePayload payload, CancellationToken ct = default)
    {
        string json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        Console.WriteLine($"[Slack mock] Kanal: {payload.Channel}");
        Console.WriteLine(json);
        return Task.CompletedTask;
    }
}
