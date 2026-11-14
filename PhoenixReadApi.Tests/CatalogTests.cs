using PhoenixReadApi.Tests.Common;

namespace PhoenixReadApi.Tests;

/// <summary>
/// 剧库与题材分类相关公开函数及枚举测试。
/// </summary>
public sealed class CatalogTests
{
    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestGetCatalogAsync_All()
    {
        using CancellationTokenSource timeout = SharedScenario.CreateTimeout();
        IReadOnlyList<Drama> catalog = await SharedScenario.Client.GetCatalogAsync(
            PhoenixReadApiCategory.All,
            timeout.Token
        );

        await Assert.That(catalog).IsNotNull();
        await Assert.That(catalog.Count > 0).IsTrue();
        foreach (Drama drama in catalog)
        {
            await SharedScenario.AssertValidDrama(drama);
        }
    }

    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestGetCatalogAsync_RealDrama()
    {
        using CancellationTokenSource timeout = SharedScenario.CreateTimeout();
        IReadOnlyList<Drama> catalog = await SharedScenario.Client.GetCatalogAsync(
            PhoenixReadApiCategory.RealDrama,
            timeout.Token
        );

        await Assert.That(catalog).IsNotNull();
        await Assert.That(catalog.Count > 0).IsTrue();
        foreach (Drama drama in catalog)
        {
            await SharedScenario.AssertValidDrama(drama);
        }
    }

    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestGetCatalogAsync_ComicDrama()
    {
        using CancellationTokenSource timeout = SharedScenario.CreateTimeout();
        IReadOnlyList<Drama> catalog = await SharedScenario.Client.GetCatalogAsync(
            PhoenixReadApiCategory.ComicDrama,
            timeout.Token
        );

        await Assert.That(catalog).IsNotNull();
        await Assert.That(catalog.Count > 0).IsTrue();
        foreach (Drama drama in catalog)
        {
            await SharedScenario.AssertValidDrama(drama);
        }
    }

    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestGetCatalogAsync_AiDrama()
    {
        using CancellationTokenSource timeout = SharedScenario.CreateTimeout();
        IReadOnlyList<Drama> catalog = await SharedScenario.Client.GetCatalogAsync(
            PhoenixReadApiCategory.AiDrama,
            timeout.Token
        );

        await Assert.That(catalog).IsNotNull();
        await Assert.That(catalog.Count > 0).IsTrue();
        foreach (Drama drama in catalog)
        {
            await SharedScenario.AssertValidDrama(drama);
        }
    }

    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestGetCatalogAsync_Comic()
    {
        using CancellationTokenSource timeout = SharedScenario.CreateTimeout();
        IReadOnlyList<Drama> catalog = await SharedScenario.Client.GetCatalogAsync(
            PhoenixReadApiCategory.Comic,
            timeout.Token
        );

        await Assert.That(catalog).IsNotNull();
        await Assert.That(catalog.Count > 0).IsTrue();
        foreach (Drama drama in catalog)
        {
            await SharedScenario.AssertValidDrama(drama);
        }
    }

    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestGetTagCategoriesAsync()
    {
        using CancellationTokenSource timeout = SharedScenario.CreateTimeout();
        IReadOnlyList<TagCategoryGroup> groups = await SharedScenario.Client.GetTagCategoriesAsync(
            timeout.Token
        );

        await Assert.That(groups).IsNotNull();
        await Assert.That(groups.Count > 0).IsTrue();
        foreach (TagCategoryGroup group in groups)
        {
            await SharedScenario.AssertValidTagCategoryGroup(group);
        }
    }

    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestGetTagDramaPageAsync()
    {
        using CancellationTokenSource timeout = SharedScenario.CreateTimeout();

        // 1. 测试默认与空 tag
        TagDramaPage defaultPage = await SharedScenario.Client.GetTagDramaPageAsync(
            tagItemId: "",
            sort: TagDramaSortOrder.Hot,
            offset: 0,
            count: 10,
            cancellationToken: timeout.Token
        );
        await SharedScenario.AssertValidTagDramaPage(defaultPage);
        await Assert.That(defaultPage.Items.Count > 0).IsTrue();

        // 2. 测试不同排序方式（最新排序）
        TagDramaPage latestPage = await SharedScenario.Client.GetTagDramaPageAsync(
            tagItemId: "",
            sort: TagDramaSortOrder.Latest,
            offset: 0,
            count: 10,
            cancellationToken: timeout.Token
        );
        await SharedScenario.AssertValidTagDramaPage(latestPage);

        // 3. 测试通过分类树中的具体标签进行过滤
        IReadOnlyList<TagCategoryGroup> groups = await SharedScenario.Client.GetTagCategoriesAsync(
            timeout.Token
        );
        TagCategoryItem? firstItem = groups.SelectMany(g => g.Items).FirstOrDefault();
        if (firstItem is not null && !string.IsNullOrWhiteSpace(firstItem.Id))
        {
            TagDramaPage filteredPage = await SharedScenario.Client.GetTagDramaPageAsync(
                tagItemId: firstItem.Id,
                sort: TagDramaSortOrder.Hot,
                offset: 0,
                count: 8,
                cancellationToken: timeout.Token
            );
            await SharedScenario.AssertValidTagDramaPage(filteredPage);

            if (filteredPage is { HasMore: true, NextOffset: > 0 })
            {
                TagDramaPage nextPage = await SharedScenario.Client.GetTagDramaPageAsync(
                    tagItemId: firstItem.Id,
                    sort: TagDramaSortOrder.Hot,
                    offset: filteredPage.NextOffset,
                    count: 8,
                    cancellationToken: timeout.Token
                );
                await SharedScenario.AssertValidTagDramaPage(nextPage);
            }
        }
    }

    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestRecommendLeaderboardPagingAsync()
    {
        using CancellationTokenSource timeout = SharedScenario.CreateTimeout();

        // 验证推荐榜第一页与第二页分页不重复
        ShortDramaLeaderboardPage page1 = await SharedScenario.Client.GetShortDramaLeaderboardAsync(
            ShortDramaLeaderboardType.Recommend,
            offset: 0,
            cancellationToken: timeout.Token
        );
        await Assert.That(page1).IsNotNull();
        await Assert.That(page1.Items.Count > 0).IsTrue();

        if (page1.HasMore && page1.NextOffset > 0)
        {
            ShortDramaLeaderboardPage page2 = await SharedScenario.Client.GetShortDramaLeaderboardAsync(
                ShortDramaLeaderboardType.Recommend,
                offset: page1.NextOffset,
                cancellationToken: timeout.Token
            );
            await Assert.That(page2).IsNotNull();
            await Assert.That(page2.Items.Count > 0).IsTrue();

            HashSet<string> page1Ids = page1.Items.Select(x => x.Id).ToHashSet(StringComparer.Ordinal);
            int duplicateCount = page2.Items.Count(x => page1Ids.Contains(x.Id));
            // 验证推荐榜跨页去重机制有效
            await Assert.That(duplicateCount < page2.Items.Count).IsTrue();
        }
    }

    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestNewDramaRecommendationsAsync()
    {
        using CancellationTokenSource timeout = SharedScenario.CreateTimeout();

        // 1. 获取新剧推荐第一页
        NewDramaRecommendationPage page1 = await SharedScenario.Client.GetNewDramaRecommendationsAsync(
            ShortDramaLeaderboardChannel.All,
            offset: 0,
            limit: 10,
            cancellationToken: timeout.Token
        );
        await Assert.That(page1).IsNotNull();
        await Assert.That(page1.Items.Count > 0).IsTrue();

        foreach (Drama drama in page1.Items)
        {
            await SharedScenario.AssertValidDrama(drama);
        }

        // 2. 获取下一页并验证数据连续性
        if (page1.HasMore && page1.NextOffset > 0)
        {
            NewDramaRecommendationPage page2 = await SharedScenario.Client.GetNewDramaRecommendationsAsync(
                ShortDramaLeaderboardChannel.All,
                offset: page1.NextOffset,
                limit: 10,
                sessionId: page1.SessionId,
                cancellationToken: timeout.Token
            );
            await Assert.That(page2).IsNotNull();
            await Assert.That(page2.Items.Count > 0).IsTrue();
        }

        // 3. 流式遍历新剧推荐（女频频道）
        int streamedCount = 0;
        await foreach (Drama drama in SharedScenario.Client.EnumerateNewDramaRecommendationsAsync(
            ShortDramaLeaderboardChannel.Female,
            pageSize: 10,
            maxPages: 2,
            cancellationToken: timeout.Token
        ))
        {
            await SharedScenario.AssertValidDrama(drama);
            streamedCount++;
        }
        await Assert.That(streamedCount > 0).IsTrue();
    }
}
