using ReviewAgent.Slack.Models;

namespace ReviewAgent.Slack;

public interface ISlackNotifier
{
    Task SendAsync(SlackMessagePayload payload, CancellationToken ct = default);
}
