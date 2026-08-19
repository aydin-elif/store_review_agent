using Google.Apis.AndroidPublisher.v3.Data;
using ReviewAgent.Connectors;
using ReviewAgent.Connectors.GooglePlay;

namespace ReviewAgent.Tests;

public class GooglePlayReviewMapperTests
{
    [Fact]
    public void MapToRawReview_ConvertsFieldsCorrectly()
    {
        Review review = new()
        {
            ReviewId = "gp-123",
            AuthorName = "test_user",
            Comments =
            [
                new()
                {
                    UserComment = new UserComment
                    {
                        StarRating = 4,
                        Text = "Test yorum",
                        LastModified = new Timestamp { Seconds = 1755000000 }
                    }
                }
            ]
        };

        RawReview result = GooglePlayReviewMapper.MapToRawReview(review);

        Assert.Equal("gp-123", result.ExternalReviewId);
        Assert.Equal(4, result.Rating);
        Assert.Equal("googleplay", result.Platform);
    }
}
