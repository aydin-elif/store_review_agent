using ReviewAgent.Data.Models;
using ReviewAgent.Data.Repositories;

namespace ReviewAgent.Data;

public static class SeedData
{
    public static async Task RunAsync(AppRepository repository)
    {
        List<AppRegistration> apps =
        [
            new()
            {
                AppKey = "bithero",
                DisplayName = "Bithero (Test)",
                AppStore = new AppStoreConfig { BundleId = "com.btcturk.bithero" },
                GooglePlay = new GooglePlayConfig { PackageName = "com.btcturk.bithero" },
                SlackChannel = "#store-reviews-test",
                IsActive = true
            },
            new()
            {
                AppKey = "kripto",
                DisplayName = "BtcTurk Kripto",
                AppStore = new AppStoreConfig { BundleId = "com.btcturk.kripto" },
                GooglePlay = new GooglePlayConfig { PackageName = "com.btcturk.kripto" },
                SlackChannel = "#store-reviews-kripto",
                IsActive = false
            },
            new()
            {
                AppKey = "hisse",
                DisplayName = "BtcTurk Hisse",
                AppStore = new AppStoreConfig { BundleId = "com.btcturk.hisse" },
                GooglePlay = new GooglePlayConfig { PackageName = "com.btcturk.hisse" },
                SlackChannel = "#store-reviews-hisse",
                IsActive = false
            },
            new()
            {
                AppKey = "global",
                DisplayName = "BtcTurk Global",
                AppStore = new AppStoreConfig { BundleId = "com.btcturk.global" },
                GooglePlay = new GooglePlayConfig { PackageName = "com.btcturk.global" },
                SlackChannel = "#store-reviews-global",
                IsActive = false
            }
        ];

        foreach (AppRegistration app in apps)
        {
            await repository.UpsertAppAsync(app);
        }
    }
}
