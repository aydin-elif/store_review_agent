using ReviewAgent.Slack.Models;

namespace ReviewAgent.Slack;

public static class CriticalAlertMessageBuilder
{
    public static SlackMessagePayload Build(string channel, CriticalAlertInfo alert)
    {
        string platformLabel = alert.Platform == "appstore" ? "App Store" : "Google Play";

        List<object> blocks =
        [
            new
            {
                type = "header",
                text = new { type = "plain_text", text = $"🚨 Kritik Yorum — {alert.AppDisplayName}" }
            },
            new
            {
                type = "section",
                text = new
                {
                    type = "mrkdwn",
                    text = $"*Platform:* {platformLabel}\n*Puan:* {alert.Rating}/5\n*Özet:* {alert.Summary}"
                }
            }
        ];

        if (!string.IsNullOrWhiteSpace(alert.ReviewUrl))
        {
            blocks.Add(new
            {
                type = "actions",
                elements = new object[]
                {
                    new
                    {
                        type = "button",
                        text = new { type = "plain_text", text = "Mağaza Sayfasını Aç" },
                        url = alert.ReviewUrl
                    }
                }
            });
        }

        return new SlackMessagePayload { Channel = channel, Blocks = blocks };
    }
}
