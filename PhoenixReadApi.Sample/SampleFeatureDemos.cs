namespace PhoenixReadApi.Sample;

internal static partial class SampleSteps
{
    /// <summary>
    /// 场景功能演示（对应红果 App 三大特色页面）：
    /// 1. 筛选找剧 (多维度题材、角色、状态筛选及排序)
    /// 2. 排行榜 (热播榜、好评榜、飙升榜、新剧榜等榜单及排名短剧)
    /// 3. 上新日历 (按日期查看已上线及即将上线新剧、预约状态)
    /// </summary>
    public static async Task RunFeatureDemosAsync(SampleContext context)
    {
        PhoenixReadApiClient client = context.Client;
        CancellationToken cancellationToken = context.CancellationToken;

        Console.WriteLine("\n==========================================");
        Console.WriteLine("场景功能演示（对应红果 App 三大特色页面）");
        Console.WriteLine("==========================================");

        // ==========================================
        // 功能展示 1: 筛选找剧 (多维度题材、角色、状态筛选及排序)
        // ==========================================
        Console.WriteLine("\n--- [功能展示 1: 筛选找剧 (标签与筛选列表)] ---");
        IReadOnlyList<TagCategoryGroup>? tagGroups = await TryAsync(
            "GetTagCategoriesAsync",
            () => client.GetTagCategoriesAsync(cancellationToken)
        );
        PrintCount("  题材分类分组数", tagGroups?.Count);
        if (tagGroups is { Count: > 0 })
        {
            foreach (TagCategoryGroup group in tagGroups.Take(3))
            {
                string tagPreview = string.Join(", ", group.Items.Take(5).Select(i => i.Name));
                Console.WriteLine(
                    $"    [{group.Name}] 包含 {group.Items.Count} 个标签: {tagPreview}..."
                );
            }

            TagCategoryItem selectedTag =
                tagGroups[0].Items.FirstOrDefault() ?? new() { Id = "", Name = "" };
            Console.WriteLine(
                $"  -> 选择标签 [{selectedTag.Name}] (Id: {selectedTag.Id})，按最热排序查询短剧:"
            );
            TagDramaPage? tagPage = await TryAsync(
                "GetTagDramaPageAsync",
                () =>
                    client.GetTagDramaPageAsync(
                        selectedTag.Id,
                        sortType: "new_sort_hot",
                        offset: 0,
                        count: 5,
                        cancellationToken: cancellationToken
                    )
            );
            PrintCount("  筛选结果数量", tagPage?.Items.Count);
            if (tagPage is not null)
            {
                foreach (Drama drama in tagPage.Items.Take(3))
                {
                    Console.WriteLine(
                        $"    - [{drama.Id}] {drama.Title} ({drama.EpisodeCount}集) - 评分: {drama.Score} 标签: {string.Join('/', drama.Tags)}"
                    );
                }
            }
        }

        // ==========================================
        // 功能展示 2: 排行榜 (热播/好评/飙升/新剧榜)
        // ==========================================
        Console.WriteLine("\n--- [功能展示 2: 排行榜 (热播/好评/飙升/新剧榜)] ---");
        FeedPage? feedCategories = await TryAsync(
            "GetFeedCategoriesAsync",
            () => client.GetFeedCategoriesAsync(cancellationToken)
        );
        PrintCount("  榜单分组数量", feedCategories?.Sections.Count);
        if (feedCategories is not null)
        {
            foreach (FeedSection section in feedCategories.Sections.Take(4))
            {
                Console.WriteLine($"  ★ 榜单: {section.Title} (共 {section.Items.Count} 部剧)");
                int rank = 1;
                foreach (Drama drama in section.Items.Take(3))
                {
                    Console.WriteLine(
                        $"     TOP {rank++}: 《{drama.Title}》 - {drama.Remark} (评分: {drama.Score})"
                    );
                }
            }
        }

        // ==========================================
        // 功能展示 3: 上新日历 (按日期查看待播新剧与预约)
        // ==========================================
        Console.WriteLine("\n--- [功能展示 3: 上新日历 (按日期查看待播新剧与预约)] ---");
        ReservationPage? upcomingList = await TryAsync(
            "GetUpcomingReservationsAsync",
            () =>
                client.GetUpcomingReservationsAsync(
                    count: 20,
                    offset: 0,
                    cancellationToken: cancellationToken
                )
        );
        context.UpcomingReservations = upcomingList;
        PrintCount("  上新日历待播剧总数", upcomingList?.Items.Count);
        if (upcomingList is not null)
        {
            var groupedByDate = upcomingList
                .Items.GroupBy(i =>
                {
                    if (i.ReleaseAtUnixSeconds <= 0)
                        return "待定";
                    DateTimeOffset date = DateTimeOffset
                        .FromUnixTimeSeconds(i.ReleaseAtUnixSeconds)
                        .ToLocalTime();
                    return date.ToString("yyyy-MM-dd");
                })
                .Take(4);
            foreach (var dateGroup in groupedByDate)
            {
                Console.WriteLine($"  📅 上映日期: {dateGroup.Key}");
                foreach (Reservation drama in dateGroup.Take(3))
                {
                    string status = drama.IsActive ? "[已上线/已开播]" : "[待上线/可预约]";
                    Console.WriteLine($"     {status} 《{drama.Title}》 (ID: {drama.SeriesId})");
                }
            }
        }
    }
}
