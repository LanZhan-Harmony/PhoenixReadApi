namespace PhoenixReadApi.Tests;

public class Tests
{
    private const string seriesId = "7687438098002611225";
    private const string videoId = "7687440021728529432";
    private static readonly PhoenixReadApiClient _client = new();

    [Test]
    public async Task TestGetCatalogAsync()
    {
        IReadOnlyList<Drama>? catalog = await _client.GetCatalogAsync(
            PhoenixReadApiCategory.RealDrama
        );
        await Assert.That(catalog).IsNotNull();
        Console.WriteLine($"  条目：{catalog.Count}");
    }

    [Test]
    public async Task TestGetDetailsAsync() { }

    [Test]
    public async Task TestResolveMediaAsync() { }

    [Test]
    public async Task TestSearchAsync() { }

    [Test]
    public async Task TestGetSuggestionsAsync() { }

    [Test]
    public async Task TestGetRecommendationsAsync() { }

    [Test]
    public async Task TestGetDanmakuAsync() { }

    [Test]
    public async Task TestGetReviewsAsync() { }

    [Test]
    public async Task TestGetEpisodeCommentsAsync() { }

    [Test]
    public async Task TestGetActorAsync() { }
}
