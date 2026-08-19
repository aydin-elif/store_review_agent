using Google.Apis.AndroidPublisher.v3;
using Google.Apis.AndroidPublisher.v3.Data;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;

namespace ReviewAgent.Connectors.GooglePlay;

public class GooglePlayReviewProvider : IReviewProvider
{
    private readonly AndroidPublisherService _service;

    public GooglePlayReviewProvider(string serviceAccountJsonPath)
    {
#pragma warning disable CS0618 // GoogleCredential.FromFile deprecated; CredentialFactory henuz stabil degil, gercek key gelince tekrar degerlendirilecek.
        GoogleCredential credential = GoogleCredential
            .FromFile(serviceAccountJsonPath)
            .CreateScoped(AndroidPublisherService.Scope.Androidpublisher);
#pragma warning restore CS0618

        _service = new AndroidPublisherService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "StoreReviewIntelligenceAgent"
        });
    }

    public async Task<List<RawReview>> FetchReviewsAsync(string appIdentifier, CancellationToken ct = default)
    {
        ReviewsResource.ListRequest request = _service.Reviews.List(appIdentifier);
        ReviewsListResponse response = await request.ExecuteAsync(ct);

        return (response.Reviews ?? new List<Review>())
            .Select(GooglePlayReviewMapper.MapToRawReview)
            .ToList();
    }
}
