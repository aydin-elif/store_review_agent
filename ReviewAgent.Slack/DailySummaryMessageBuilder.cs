using ReviewAgent.Slack.Models;

namespace ReviewAgent.Slack;

public static class DailySummaryMessageBuilder
{
    public static SlackMessagePayload Build(string channel, DailySummaryStats stats)
    {
        List<object> blocks =
        [
            new
            {
                type = "header",
                text = new { type = "plain_text", text = $"📊 {stats.AppDisplayName} - Günlük Özet" }
            },
            new
            {
                type = "section",
                fields = new object[]
                {
                    new { type = "mrkdwn", text = $"*Toplam Yorum:*\n{stats.TotalReviews}" },
                    new { type = "mrkdwn", text = $"*En Çok Bahsedilen Kategori:*\n{stats.TopCategory}" },
                    new { type = "mrkdwn", text = $"*Pozitif:*\n{stats.PositiveCount}" },
                    new { type = "mrkdwn", text = $"*Negatif:*\n{stats.NegativeCount}" }
                }
            },
            new { type = "divider" }
        ];

        if (stats.TopPriorityReviews.Count > 0)
        {
            blocks.Add(new
            {
                type = "section",
                text = new { type = "mrkdwn", text = "*Öncelikli Yorumlar:*" }
            });

            foreach (TopReview review in stats.TopPriorityReviews.Take(5))
            {
                blocks.Add(new
                {
                    type = "section",
                    text = new
                    {
                        type = "mrkdwn",
                        text = $"⭐ {review.Rating}/5 (öncelik: {review.PriorityScore}) — {review.Summary}"
                    }
                });
            }
        }

        return new SlackMessagePayload { Channel = channel, Blocks = blocks };
    }
}
