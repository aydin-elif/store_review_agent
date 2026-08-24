using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ReviewAgent.Slack.Models;

namespace ReviewAgent.Slack;

public class SlackApiNotifier : ISlackNotifier
{
    private readonly HttpClient _httpClient;

    public SlackApiNotifier(HttpClient httpClient, string botToken)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("https://slack.com/api/");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", botToken);
    }

    public async Task SendAsync(SlackMessagePayload payload, CancellationToken ct = default)
    {
        string json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        StringContent content = new(json, Encoding.UTF8, "application/json");

        HttpResponseMessage response = await _httpClient.PostAsync("chat.postMessage", content, ct);
        response.EnsureSuccessStatusCode();

        string responseBody = await response.Content.ReadAsStringAsync(ct);
        using JsonDocument doc = JsonDocument.Parse(responseBody);

        if (!doc.RootElement.GetProperty("ok").GetBoolean())
        {
            string error = doc.RootElement.TryGetProperty("error", out JsonElement errProp)
                ? errProp.GetString() ?? "bilinmeyen hata"
                : "bilinmeyen hata";
            throw new InvalidOperationException($"Slack API hatası: {error}");
        }
    }
}
