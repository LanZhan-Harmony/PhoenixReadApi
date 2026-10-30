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
        IReadOnlyList<TagCategoryGroup> groups = await SharedScenario.Client.GetTagCategoriesAsync(timeout.Token);

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
            sortType: "new_sort_hot",
            offset: 0,
            count: 10,
            cancellationToken: timeout.Token
        );
        await SharedScenario.AssertValidTagDramaPage(defaultPage);
        await Assert.That(defaultPage.Items.Count > 0).IsTrue();

        // 2. 测试不同排序方式（最新排序）
        TagDramaPage latestPage = await SharedScenario.Client.GetTagDramaPageAsync(
            tagItemId: "",
            sortType: "new_sort_latest",
            offset: 0,
            count: 10,
            cancellationToken: timeout.Token
        );
        await SharedScenario.AssertValidTagDramaPage(latestPage);

        // 3. 测试通过分类树中的具体标签进行过滤
        IReadOnlyList<TagCategoryGroup> groups = await SharedScenario.Client.GetTagCategoriesAsync(timeout.Token);
        TagCategoryItem? firstItem = groups.SelectMany(g => g.Items).FirstOrDefault();
        if (firstItem is not null && !string.IsNullOrWhiteSpace(firstItem.Id))
        {
            TagDramaPage filteredPage = await SharedScenario.Client.GetTagDramaPageAsync(
                tagItemId: firstItem.Id,
                sortType: "new_sort_hot",
                offset: 0,
                count: 8,
                cancellationToken: timeout.Token
            );
            await SharedScenario.AssertValidTagDramaPage(filteredPage);

            if (filteredPage.HasMore && filteredPage.NextOffset > 0)
            {
                TagDramaPage nextPage = await SharedScenario.Client.GetTagDramaPageAsync(
                    tagItemId: firstItem.Id,
                    sortType: "new_sort_hot",
                    offset: filteredPage.NextOffset,
                    count: 8,
                    cancellationToken: timeout.Token
                );
                await SharedScenario.AssertValidTagDramaPage(nextPage);
            }
        }
    }
}
