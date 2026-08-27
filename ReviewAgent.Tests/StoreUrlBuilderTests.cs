using ReviewAgent.Data.Models;
using ReviewAgent.Worker.Jobs;

namespace ReviewAgent.Tests;

public class StoreUrlBuilderTests
{
    [Fact]
    public void Build_GooglePlay_ReturnsCorrectUrl()
    {
        AppRegistration app = new()
        {
            GooglePlay = new GooglePlayConfig { PackageName = "com.btcturk.pro" }
        };

        string? url = StoreUrlBuilder.Build(app, "googleplay");

        Assert.Equal("https://play.google.com/store/apps/details?id=com.btcturk.pro&reviewId=all", url);
    }

    [Fact]
    public void Build_MissingConfig_ReturnsNull()
    {
        AppRegistration app = new() { GooglePlay = null };

        string? url = StoreUrlBuilder.Build(app, "googleplay");

        Assert.Null(url);
    }
}
