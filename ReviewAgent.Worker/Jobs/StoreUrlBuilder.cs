using ReviewAgent.Data.Models;

namespace ReviewAgent.Worker.Jobs;

public static class StoreUrlBuilder
{
    public static string? Build(AppRegistration app, string platform)
    {
        return platform switch
        {
            "googleplay" when app.GooglePlay is not null =>
                $"https://play.google.com/store/apps/details?id={app.GooglePlay.PackageName}&reviewId=all",
            "appstore" when app.AppStore is not null =>
                $"https://apps.apple.com/app/id{app.AppStore.AppId}?action=write-review",
            _ => null
        };
    }
}
