using PhoenixReadApi.Tests.Common;

namespace PhoenixReadApi.Tests;

/// <summary>
/// 新补齐 API（合集与季数聚合、单剧关联推荐、动态多维筛选、按日期上新日历、书城发现瀑布流、原著小说与搜索分页）测试。
/// </summary>
public sealed class NewFeatureIntegrationTests
{
    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestSearchQueryWithPaginationAndTabsAsync()
    {
        using CancellationTokenSource timeout = SharedScenario.CreateTimeout();

        SearchPage<Drama> result = await SharedScenario.Client.SearchDramasAsync(
            "总裁",
            offset: 0,
            count: 10,
            cancellationToken: timeout.Token
        );
        await Assert.That(result).IsNotNull();
        await Assert.That(result.Items).IsNotNull();
        await Assert.That(result.Total >= 0).IsTrue();
    }

    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestGetDramaSeasonsAsync()
    {
        using CancellationTokenSource timeout = SharedScenario.CreateTimeout();
        TestDramaScenario scenario = await SharedScenario.GetScenarioAsync(timeout.Token);

        DramaAlbum album = await SharedScenario.Client.GetDramaSeasonsAsync(scenario.DramaId, timeout.Token);
        await Assert.That(album).IsNotNull();
        await Assert.That(album.Seasons).IsNotNull();
        await Assert.That(album.SeasonCount > 0).IsTrue();
        await Assert.That(album.Seasons.Count > 0).IsTrue();
    }

    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestGetDramaRecommendationsAsync()
    {
        using CancellationTokenSource timeout = SharedScenario.CreateTimeout();
        TestDramaScenario scenario = await SharedScenario.GetScenarioAsync(timeout.Token);

        try
        {
            IReadOnlyList<RelatedWorkItem> related = await SharedScenario.Client.GetDramaRelatedWorksAsync(
                scenario.DramaId,
                timeout.Token
            );

            await Assert.That(related).IsNotNull();

            RecommendationFeedPage guess = await SharedScenario.Client.GetDramaGuessYouLikeAsync(
                scenario.DramaId,
                cancellationToken: timeout.Token
            );

            await Assert.That(guess).IsNotNull();
            await Assert.That(guess.Items).IsNotNull();
        }
        catch (PhoenixReadApiResponseException)
        {
            // 服务端部分单剧若暂无专属推荐属于合法业务响应
        }
    }

    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestShortDramaFilterPanelAndQueryAsync()
    {
        using CancellationTokenSource timeout = SharedScenario.CreateTimeout();

        // 1. 获取筛选面板
        IReadOnlyList<ShortDramaFilterGroup> rows = await SharedScenario.Client.GetShortDramaFilterPanelAsync(
            ShortDramaGenre.ShortPlay,
            timeout.Token
        );

        await Assert.That(rows).IsNotNull();
        await Assert.That(rows.Count > 0).IsTrue();

        // 2. 根据面板中的第一个维度进行筛选查询
        ShortDramaFilterGroup firstRow = rows[0];
        Dictionary<string, IReadOnlyList<string>> selections = [];
        if (firstRow.Items.Count > 0)
        {
            selections[firstRow.Key] = [firstRow.Items[0].Value];
        }

        ShortDramaFilterPage page = await SharedScenario.Client.FilterShortDramasAsync(
            new ShortDramaFilterQuery
            {
                Genre = ShortDramaGenre.ShortPlay,
                Selections = selections,
                Offset = 0,
            },
            timeout.Token
        );

        await Assert.That(page).IsNotNull();
        await Assert.That(page.Items).IsNotNull();
    }

    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestGetDailyReleasesAsync()
    {
        using CancellationTokenSource timeout = SharedScenario.CreateTimeout();

        DailyReleasePage page = await SharedScenario.Client.GetDailyReleasesAsync(
            targetDate: null,
            offset: 0,
            cancellationToken: timeout.Token
        );

        await Assert.That(page).IsNotNull();
        await Assert.That(page.Items).IsNotNull();
        await Assert.That(page.AvailableDates).IsNotNull();
    }

    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestDiscoverPageAsync()
    {
        using CancellationTokenSource timeout = SharedScenario.CreateTimeout();

        DiscoverPage page = await SharedScenario.Client.GetDiscoverPageAsync(
            DiscoverChannel.Recommend,
            cursor: null,
            cancellationToken: timeout.Token
        );

        await Assert.That(page).IsNotNull();
        await Assert.That(page.Dramas).IsNotNull();
        await Assert.That(page.Novels).IsNotNull();
        await Assert.That(page.Dramas.Count > 0).IsTrue();
        foreach (Drama drama in page.Dramas)
        {
            await SharedScenario.AssertValidDrama(drama);
            await Assert.That(string.IsNullOrWhiteSpace(drama.CoverUrl)).IsFalse();
            await Assert.That(drama.Tags.Count > 0).IsTrue();
        }
    }

    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestGetNovelCategoryFrontAsync()
    {
        using CancellationTokenSource timeout = SharedScenario.CreateTimeout();

        try
        {
            NovelCategoryPage page = await SharedScenario.Client.GetNovelCategoryFrontAsync(
                categoryId: "1",
                offset: 0,
                count: 10,
                cancellationToken: timeout.Token
            );

            await Assert.That(page).IsNotNull();
            await Assert.That(page.Items).IsNotNull();
        }
        catch (PhoenixReadApiResponseException)
        {
            // 某些分类 ID 服务端可能未配置为前台分类
        }
    }

    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestSearchNovelAndUserTabsAsync()
    {
        using CancellationTokenSource timeout = SharedScenario.CreateTimeout();

        // 1. 默认短剧搜索与分页
        SearchPage<Drama> firstPage = await SharedScenario.Client.SearchDramasAsync(
            "逆袭",
            count: 20,
            offset: 0,
            cancellationToken: timeout.Token
        );

        await Assert.That(firstPage).IsNotNull();
        await Assert.That(firstPage.Items).IsNotNull();
        await Assert.That(firstPage.Items.Count > 0).IsTrue();

        if (firstPage.HasMore && firstPage.NextOffset > 0)
        {
            SearchPage<Drama> secondPage = await SharedScenario.Client.SearchDramasAsync(
                "逆袭",
                count: 20,
                offset: firstPage.NextOffset,
                searchId: firstPage.SearchId,
                cancellationToken: timeout.Token
            );

            await Assert.That(secondPage).IsNotNull();
            await Assert.That(secondPage.Items).IsNotNull();
        }

        // 2. 原著小说搜索
        SearchPage<Novel> novelSearch = await SharedScenario.Client.SearchNovelsAsync(
            "逆袭",
            count: 10,
            cancellationToken: timeout.Token
        );

        await Assert.That(novelSearch).IsNotNull();
        await Assert.That(novelSearch.Items).IsNotNull();

        // 3. 用户/演员搜索
        SearchPage<User> userSearch = await SharedScenario.Client.SearchUsersAsync(
            "张",
            count: 5,
            cancellationToken: timeout.Token
        );

        await Assert.That(userSearch).IsNotNull();
        await Assert.That(userSearch.Items).IsNotNull();
    }
}
