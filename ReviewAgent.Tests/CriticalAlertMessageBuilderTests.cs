using ReviewAgent.Slack;
using ReviewAgent.Slack.Models;

namespace ReviewAgent.Tests;

public class CriticalAlertMessageBuilderTests
{
    [Fact]
    public void Build_WithReviewUrl_IncludesActionButton()
    {
        CriticalAlertInfo alert = new()
        {
            AppDisplayName = "Bithero (Test)",
            Platform = "appstore",
            Rating = 1,
            Summary = "Kullanıcı çökme bildiriyor",
            ReviewUrl = "https://apps.apple.com/review/123"
        };

        SlackMessagePayload payload = CriticalAlertMessageBuilder.Build("#store-reviews-test", alert);

        Assert.Equal("#store-reviews-test", payload.Channel);
        Assert.Equal(3, payload.Blocks.Count);
    }
}
