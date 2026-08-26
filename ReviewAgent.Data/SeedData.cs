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
                AppKey = "kripto",
                DisplayName = "BtcTurk Kripto",
                AppStore = null,
                GooglePlay = new GooglePlayConfig
                {
                    PackageName = "com.btcturk.pro",
                    ServiceAccountSecretRef = "secrets/google-play-service-account.json"
                },
                SlackChannel = "C0BRT0FG831",
                IsActive = true
            },
            new()
            {
                AppKey = "hisse",
                DisplayName = "BtcTurk Hisse",
                AppStore = null,
                GooglePlay = new GooglePlayConfig
                {
                    PackageName = "com.btcturk.invest",
                    ServiceAccountSecretRef = "secrets/google-play-service-account.json"
                },
                SlackChannel = "C0BRT0FG831",
                IsActive = true
            },
            new()
            {
                AppKey = "bithero",
                DisplayName = "Bithero (Test)",
                AppStore = new AppStoreConfig { BundleId = "com.btcturk.bithero" },
                GooglePlay = new GooglePlayConfig { PackageName = "com.btcturk.bithero" },
                SlackChannel = "C0BRT0FG831",
                IsActive = false
            },
            new()
            {
                AppKey = "global",
                DisplayName = "BtcTurk Global",
                AppStore = new AppStoreConfig { BundleId = "com.btcturk.global" },
                GooglePlay = new GooglePlayConfig { PackageName = "com.btcturk.global" },
                SlackChannel = "C0BRT0FG831",
                IsActive = false
            }
        ];

        foreach (AppRegistration app in apps)
        {
            await repository.UpsertAppAsync(app);
        }
    }
}
