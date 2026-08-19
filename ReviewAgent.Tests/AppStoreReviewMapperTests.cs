using ReviewAgent.Connectors;
using ReviewAgent.Connectors.AppStore;

namespace ReviewAgent.Tests;

public class AppStoreReviewMapperTests
{
    [Fact]
    public void MapToRawReview_ConvertsFieldsCorrectly()
    {
        AppStoreReviewData data = new()
        {
            Id = "abc123",
            Attributes = new AppStoreReviewAttributes
            {
                Rating = 4,
                Title = "Test başlık",
                Body = "Test yorum metni",
                ReviewerNickname = "test_user",
                CreatedDate = new DateTime(2026, 8, 15)
            }
        };

        RawReview result = AppStoreReviewMapper.MapToRawReview(data);

        Assert.Equal("abc123", result.ExternalReviewId);
        Assert.Equal(4, result.Rating);
        Assert.Equal("appstore", result.Platform);
    }
}
