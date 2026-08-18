using Google.Apis.AndroidPublisher.v3;
using Google.Apis.AndroidPublisher.v3.Data;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;

namespace ReviewAgent.Connectors.GooglePlay;

public class GooglePlayReviewFetcher
{
    private readonly AndroidPublisherService _service;

    public GooglePlayReviewFetcher(string serviceAccountJsonPath)
    {
        GoogleCredential credential = GoogleCredential
            .FromFile(serviceAccountJsonPath)
            .CreateScoped(AndroidPublisherService.Scope.Androidpublisher);

        _service = new AndroidPublisherService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "StoreReviewIntelligenceAgent"
        });
    }

    public async Task<List<Review>> FetchReviewsAsync(string packageName, CancellationToken ct = default)
    {
        ReviewsResource.ListRequest request = _service.Reviews.List(packageName);
        ReviewsListResponse response = await request.ExecuteAsync(ct);
        return response.Reviews?.ToList() ?? new List<Review>();
    }
}
