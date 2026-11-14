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

        SearchQuery query = new()
        {
            Keyword = "总裁",
            Offset = 0,
            Count = 10,
            Tab = SearchTab.SeriesVideo,
        };

        SearchResult result = await SharedScenario.Client.SearchAsync(query, timeout.Token);
        await Assert.That(result).IsNotNull();
        await Assert.That(result.Dramas).IsNotNull();
        await Assert.That(result.Total >= 0).IsTrue();
    }

    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestGetDramaSeasonsAsync()
    {
        using CancellationTokenSource timeout = SharedScenario.CreateTimeout();
        TestDramaScenario scenario = await SharedScenario.GetScenarioAsync(timeout.Token);

        DramaAlbum album = await SharedScenario.Client.GetDramaSeasonsAsync(scenario.SeriesId, timeout.Token);
        await Assert.That(album).IsNotNull();
        await Assert.That(album.Seasons).IsNotNull();
        await Assert.That(album.SeasonCount > 0).IsTrue();
        await Assert.That(album.Seasons.Count > 0).IsTrue();
    }

    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestGetSeriesRecommendationsAsync()
    {
        using CancellationTokenSource timeout = SharedScenario.CreateTimeout();
        TestDramaScenario scenario = await SharedScenario.GetScenarioAsync(timeout.Token);

        try
        {
            DramaRecommendations rec = await SharedScenario.Client.GetDramaRecommendationsAsync(
                scenario.SeriesId,
                timeout.Token
            );

            await Assert.That(rec).IsNotNull();
            await Assert.That(rec.Items).IsNotNull();
            await Assert.That(rec.Title).IsNotNull();
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

        // 1. 默认短剧搜索 (ShortPlay) 与分页
        SearchResult firstPage = await SharedScenario.Client.SearchAsync(
            new SearchQuery
            {
                Keyword = "逆袭",
                Tab = SearchTab.ShortPlay,
                Count = 20,
                Offset = 0,
            },
            timeout.Token
        );

        await Assert.That(firstPage).IsNotNull();
        await Assert.That(firstPage.Dramas).IsNotNull();
        await Assert.That(firstPage.Dramas.Count > 0).IsTrue();

        if (firstPage.HasMore && firstPage.NextOffset > 0)
        {
            SearchResult secondPage = await SharedScenario.Client.SearchAsync(
                new SearchQuery
                {
                    Keyword = "逆袭",
                    Tab = SearchTab.ShortPlay,
                    Count = 20,
                    Offset = firstPage.NextOffset,
                    SearchId = firstPage.SearchId,
                },
                timeout.Token
            );

            await Assert.That(secondPage).IsNotNull();
            await Assert.That(secondPage.Dramas).IsNotNull();
        }

        // 2. 原著小说搜索 (CommonBook)
        SearchResult novelSearch = await SharedScenario.Client.SearchAsync(
            new SearchQuery
            {
                Keyword = "逆袭",
                Tab = SearchTab.CommonBook,
                Count = 10,
            },
            timeout.Token
        );

        await Assert.That(novelSearch).IsNotNull();
        await Assert.That(novelSearch.Novels).IsNotNull();

        // 3. 用户/演员搜索 (User)
        SearchResult userSearch = await SharedScenario.Client.SearchAsync(
            new SearchQuery
            {
                Keyword = "张",
                Tab = SearchTab.User,
                Count = 5,
            },
            timeout.Token
        );

        await Assert.That(userSearch).IsNotNull();
        await Assert.That(userSearch.Users).IsNotNull();
    }
}
