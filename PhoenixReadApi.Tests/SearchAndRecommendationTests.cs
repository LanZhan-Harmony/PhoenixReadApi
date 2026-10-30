using PhoenixReadApi.Tests.Common;

namespace PhoenixReadApi.Tests;

/// <summary>
/// 搜索、联想建议与推荐相关公开函数及枚举测试。
/// </summary>
public sealed class SearchAndRecommendationTests
{
    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestSearchAsync()
    {
        using CancellationTokenSource timeout = SharedScenario.CreateTimeout();
        SearchResult search = await SharedScenario.Client.SearchAsync(
            SharedScenario.DefaultKeyword,
            timeout.Token
        );

        await Assert.That(search).IsNotNull();
        await Assert.That(search.Items).IsNotNull();
        await Assert.That(search.Items.Count > 0).IsTrue();
        await Assert.That(search.Total >= search.Items.Count).IsTrue();
        await Assert.That(search.Warning).IsNotNull();

        foreach (Drama drama in search.Items)
        {
            await SharedScenario.AssertValidDrama(drama);
        }

        await Assert
            .That(search.Items.Any(item =>
                item.Title.Contains(SharedScenario.DefaultKeyword, StringComparison.OrdinalIgnoreCase)
            ))
            .IsTrue();
    }

    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestGetSuggestionsAsync()
    {
        using CancellationTokenSource timeout = SharedScenario.CreateTimeout();

        IReadOnlyList<SearchSuggestion> suggestions = await SharedScenario.Client.GetSuggestionsAsync(
            SharedScenario.DefaultKeyword,
            maxResults: 5,
            cancellationToken: timeout.Token
        );

        await Assert.That(suggestions).IsNotNull();
        await Assert.That(suggestions.Count > 0).IsTrue();
        await Assert.That(suggestions.Count <= 5).IsTrue();

        foreach (SearchSuggestion suggestion in suggestions)
        {
            await Assert.That(suggestion).IsNotNull();
            await Assert.That(string.IsNullOrWhiteSpace(suggestion.Name)).IsFalse();
            await Assert.That(Enum.IsDefined(typeof(SearchSuggestionType), suggestion.Type)).IsTrue();
        }

        // 测试请求超额数量时的截断边界
        IReadOnlyList<SearchSuggestion> clampedSuggestions = await SharedScenario.Client.GetSuggestionsAsync(
            SharedScenario.DefaultKeyword,
            maxResults: 50,
            cancellationToken: timeout.Token
        );
        await Assert.That(clampedSuggestions).IsNotNull();
        await Assert.That(clampedSuggestions.Count <= 20).IsTrue();
    }

    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestGetRecommendationsAsync_ShortDrama()
    {
        using CancellationTokenSource timeout = SharedScenario.CreateTimeout();

        RecommendationPage page = await SharedScenario.Client.GetRecommendationsAsync(
            RecommendationGenre.ShortDrama,
            offset: 0,
            sessionId: null,
            seen: null,
            cancellationToken: timeout.Token
        );

        await Assert.That(page).IsNotNull();
        await Assert.That(page.Items).IsNotNull();
        await Assert.That(page.Items.Count > 0).IsTrue();
        await Assert.That(page.SessionId).IsNotNull();
        await Assert.That(page.NextOffset >= 0).IsTrue();

        foreach (Drama drama in page.Items)
        {
            await SharedScenario.AssertValidDrama(drama);
        }
    }

    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestGetRecommendationsAsync_ComicDrama()
    {
        using CancellationTokenSource timeout = SharedScenario.CreateTimeout();

        RecommendationPage page = await SharedScenario.Client.GetRecommendationsAsync(
            RecommendationGenre.ComicDrama,
            offset: 0,
            sessionId: null,
            seen: null,
            cancellationToken: timeout.Token
        );

        await Assert.That(page).IsNotNull();
        await Assert.That(page.Items).IsNotNull();
        await Assert.That(page.Items.Count > 0).IsTrue();
        await Assert.That(page.SessionId).IsNotNull();
        await Assert.That(page.NextOffset >= 0).IsTrue();

        foreach (Drama drama in page.Items)
        {
            await SharedScenario.AssertValidDrama(drama);
        }
    }

    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestGetRecommendationsAsync_AiDrama()
    {
        using CancellationTokenSource timeout = SharedScenario.CreateTimeout();

        RecommendationPage page = await SharedScenario.Client.GetRecommendationsAsync(
            RecommendationGenre.AiDrama,
            offset: 0,
            sessionId: null,
            seen: null,
            cancellationToken: timeout.Token
        );

        await Assert.That(page).IsNotNull();
        await Assert.That(page.Items).IsNotNull();
        await Assert.That(page.Items.Count > 0).IsTrue();
        await Assert.That(page.SessionId).IsNotNull();
        await Assert.That(page.NextOffset >= 0).IsTrue();

        foreach (Drama drama in page.Items)
        {
            await SharedScenario.AssertValidDrama(drama);
        }
    }

    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestGetRecommendationsAsync_Pagination()
    {
        using CancellationTokenSource timeout = SharedScenario.CreateTimeout();

        RecommendationPage firstPage = await SharedScenario.Client.GetRecommendationsAsync(
            RecommendationGenre.ShortDrama,
            offset: 0,
            cancellationToken: timeout.Token
        );

        await Assert.That(firstPage.Items.Count > 0).IsTrue();

        // 请求第二页，带上 sessionId 和 seen 列表
        RecommendationPage secondPage = await SharedScenario.Client.GetRecommendationsAsync(
            RecommendationGenre.ShortDrama,
            offset: firstPage.NextOffset,
            sessionId: firstPage.SessionId,
            seen: firstPage.Items.Select(static item => item.Id),
            cancellationToken: timeout.Token
        );

        await Assert.That(secondPage).IsNotNull();
        await Assert.That(secondPage.Items).IsNotNull();
        await Assert.That(secondPage.SessionId).IsNotNull();
        await Assert.That(secondPage.NextOffset >= 0).IsTrue();

        foreach (Drama drama in secondPage.Items)
        {
            await SharedScenario.AssertValidDrama(drama);
        }
    }
}
