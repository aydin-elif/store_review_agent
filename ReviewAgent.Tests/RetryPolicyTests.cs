using ReviewAgent.Connectors.Resilience;

namespace ReviewAgent.Tests;

public class RetryPolicyTests
{
    [Fact]
    public async Task RetryPolicy_RetriesOnHttpRequestException_ThenSucceeds()
    {
        Polly.Retry.AsyncRetryPolicy policy = RetryPolicies.CreateDefaultRetryPolicy("test");
        int attemptCount = 0;

        string result = await policy.ExecuteAsync(async () =>
        {
            attemptCount++;
            if (attemptCount < 3)
            {
                throw new HttpRequestException("Simüle edilmiş geçici hata");
            }

            await Task.CompletedTask;
            return "başarılı";
        });

        Assert.Equal("başarılı", result);
        Assert.Equal(3, attemptCount);
    }

    [Fact]
    public async Task RetryPolicy_GivesUpAfterMaxRetries()
    {
        Polly.Retry.AsyncRetryPolicy policy = RetryPolicies.CreateDefaultRetryPolicy("test");
        int attemptCount = 0;

        await Assert.ThrowsAsync<HttpRequestException>(async () =>
        {
            await policy.ExecuteAsync<string>(async () =>
            {
                attemptCount++;
                throw new HttpRequestException("Kalıcı hata");
            });
        });

        Assert.Equal(4, attemptCount);
    }
}
