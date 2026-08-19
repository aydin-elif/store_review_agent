using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace ReviewAgent.Connectors.AppStore;

public class AppStoreReviewProvider : IReviewProvider
{
    private readonly HttpClient _httpClient;
    private readonly AppStoreJwtGenerator _jwtGenerator;

    public AppStoreReviewProvider(HttpClient httpClient, AppStoreJwtGenerator jwtGenerator)
    {
        _httpClient = httpClient;
        _jwtGenerator = jwtGenerator;
    }

    public async Task<List<RawReview>> FetchReviewsAsync(string appIdentifier, CancellationToken ct = default)
    {
        string token = _jwtGenerator.GenerateToken();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        string url = $"https://api.appstoreconnect.apple.com/v1/apps/{appIdentifier}/customerReviews?sort=-createdDate";

        AppStoreReviewsResponse? response = await _httpClient.GetFromJsonAsync<AppStoreReviewsResponse>(url, ct);

        return (response?.Data ?? new List<AppStoreReviewData>())
            .Select(AppStoreReviewMapper.MapToRawReview)
            .ToList();
    }
}
