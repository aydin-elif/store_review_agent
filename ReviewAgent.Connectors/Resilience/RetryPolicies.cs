using Polly;
using Polly.Retry;

namespace ReviewAgent.Connectors.Resilience;

public static class RetryPolicies
{
    /// <summary>
    /// Geçici hatalarda (ağ sorunu, 5xx, rate limit) üstel bekleme ile
    /// 3 kez yeniden dener: 2sn, 4sn, 8sn.
    /// </summary>
    public static AsyncRetryPolicy CreateDefaultRetryPolicy(string providerName)
    {
        return Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                onRetry: (exception, delay, attempt, _) =>
                {
                    Console.WriteLine(
                        $"[{providerName}] Deneme {attempt} başarısız ({exception.GetType().Name}: {exception.Message}), {delay.TotalSeconds}sn sonra tekrar denenecek.");
                });
    }
}
