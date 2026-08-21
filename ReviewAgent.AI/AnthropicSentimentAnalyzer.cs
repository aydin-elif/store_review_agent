using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using ReviewAgent.AI.Models;
using ReviewAgent.AI.Prompts;

namespace ReviewAgent.AI;

public class AnthropicSentimentAnalyzer : ISentimentAnalyzer
{
    private const string Model = "claude-sonnet-4-6";

    private readonly HttpClient _httpClient;
    private readonly AsyncRetryPolicy _retryPolicy;

    public AnthropicSentimentAnalyzer(HttpClient httpClient, string apiKey, ILogger<AnthropicSentimentAnalyzer>? logger = null)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("https://api.anthropic.com/");
        _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
        _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

        _retryPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                onRetry: (exception, delay, attempt, _) =>
                {
                    if (logger is not null)
                    {
                        logger.LogWarning(
                            "[{ProviderName}] Deneme {Attempt} başarısız ({ExceptionType}: {ExceptionMessage}), {Delay}sn sonra tekrar denenecek.",
                            nameof(AnthropicSentimentAnalyzer),
                            attempt,
                            exception.GetType().Name,
                            exception.Message,
                            delay.TotalSeconds);
                    }
                    else
                    {
                        Console.WriteLine(
                            $"[{nameof(AnthropicSentimentAnalyzer)}] Deneme {attempt} başarısız ({exception.GetType().Name}: {exception.Message}), {delay.TotalSeconds}sn sonra tekrar denenecek.");
                    }
                });
    }

    public async Task<ReviewAnalysisResult> AnalyzeAsync(
        string reviewTitle,
        string reviewBody,
        int rating,
        CancellationToken ct = default)
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            string json = JsonSerializer.Serialize(new
            {
                model = Model,
                max_tokens = 300,
                system = AnalysisPrompt.SystemPrompt,
                messages = new[]
                {
                    new { role = "user", content = AnalysisPrompt.BuildUserPrompt(reviewTitle, reviewBody, rating) }
                }
            });
            StringContent content = new(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await _httpClient.PostAsync("v1/messages", content, ct);
            response.EnsureSuccessStatusCode();

            string responseJson = await response.Content.ReadAsStringAsync(ct);
            return ParseResponse(responseJson);
        });
    }

    private static ReviewAnalysisResult ParseResponse(string responseJson)
    {
        using JsonDocument doc = JsonDocument.Parse(responseJson);
        string text = doc.RootElement.GetProperty("content")[0].GetProperty("text").GetString() ?? "{}";

        using JsonDocument innerDoc = JsonDocument.Parse(text);
        JsonElement root = innerDoc.RootElement;

        return new ReviewAnalysisResult
        {
            Sentiment = root.GetProperty("sentiment").GetString() ?? "neutral",
            Category = root.GetProperty("category").GetString() ?? "other",
            PriorityScore = root.GetProperty("priority_score").GetInt32(),
            Summary = root.GetProperty("summary").GetString() ?? string.Empty,
            ModelVersion = Model
        };
    }
}
