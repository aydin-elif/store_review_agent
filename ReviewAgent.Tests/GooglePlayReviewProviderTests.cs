using ReviewAgent.Connectors.GooglePlay;

namespace ReviewAgent.Tests;

public class GooglePlayReviewProviderTests
{
    [Fact]
    public void Constructor_WithMissingFile_ThrowsException()
    {
        string missingPath = "nonexistent-service-account.json";

        Assert.ThrowsAny<Exception>(() => new GooglePlayReviewProvider(missingPath));
    }
}
