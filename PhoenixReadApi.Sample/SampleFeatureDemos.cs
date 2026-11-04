namespace PhoenixReadApi.Sample;

internal static partial class SampleSteps
{
    /// <summary>
    /// 场景功能演示（对应红果 App 各大特色页面与派生能力）：
    /// 1. 筛选找剧 (多维度题材、角色、状态筛选及排序)
    /// 2. 短剧多维动态筛选面板与条件矩阵 (ShortFilterPanel & ShortFilter)
    /// 3. 排行榜 (热播榜、好评榜、飙升榜、新剧榜等榜单及排名短剧)
    /// 4. 上新日历与待播预约 (按日期查看已上线及即将上线新剧、预约状态)
    /// 5. 按日期指定查询上新日历 (Daily NewReleases)
    /// 6. App 首页发现频道流式瀑布流 (DiscoverPage)
    /// 7. 多季合集与季数聚合 (Album & Seasons)
    /// 8. 单剧专属关联好剧推荐 (SeriesRecommend)
    /// 9. 跨模态搜索 (短剧、原著小说、明星演员)
    /// </summary>
    public static async Task RunFeatureDemosAsync(SampleContext context)
    {
        PhoenixReadApiClient client = context.Client;
        CancellationToken cancellationToken = context.CancellationToken;

        Console.WriteLine("\n==========================================");
        Console.WriteLine("场景功能演示（对应红果 App 各大核心特色页面）");
        Console.WriteLine("==========================================");

        // ==========================================
        // 功能展示 1: 筛选找剧 (标签分类大类与筛选列表)
        // ==========================================
        Console.WriteLine("\n--- [功能展示 1: 筛选找剧 (标签分类)] ---");
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
                        sort: TagDramaSortOrder.Hot,
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
        // 功能展示 2: 短剧多维动态筛选面板与多维矩阵筛选
        // ==========================================
        Console.WriteLine("\n--- [功能展示 2: 短剧多维动态筛选面板 (ShortFilterPanel)] ---");
        IReadOnlyList<ShortDramaFilterRow>? filterRows = await TryAsync(
            "GetShortDramaFilterPanelAsync",
            () => client.GetShortDramaFilterPanelAsync(ShortDramaGenre.ShortPlay, cancellationToken)
        );
        PrintCount("  动态筛选维度行数", filterRows?.Count);
        if (filterRows is { Count: > 0 })
        {
            Dictionary<string, IReadOnlyList<string>> selections = [];
            foreach (ShortDramaFilterRow row in filterRows.Take(3))
            {
                string optPreview = string.Join(", ", row.Options.Take(4).Select(o => o.Name));
                Console.WriteLine($"    [{row.Name} ({row.Key})]: {optPreview}...");
                if (row.Options.Count > 0 && selections.Count == 0)
                {
                    selections[row.Key] = [row.Options[0].Value];
                }
            }

            Console.WriteLine("  -> 组合筛选矩阵执行筛选查询:");
            ShortDramaFilterPage? filterPage = await TryAsync(
                "FilterShortDramasAsync",
                () =>
                    client.FilterShortDramasAsync(
                        new ShortDramaFilterQuery
                        {
                            Genre = ShortDramaGenre.ShortPlay,
                            Selections = selections,
                            Offset = 0,
                        },
                        cancellationToken
                    )
            );
            PrintCount("  多维筛选结果数量", filterPage?.Items.Count);
        }

        // ==========================================
        // 功能展示 3: 短剧排行榜 (真人剧·热播榜)
        // ==========================================
        Console.WriteLine("\n--- [功能展示 3: 短剧排行榜 (真人剧·热播榜)] ---");
        ShortDramaLeaderboardPage? leaderboard = await TryAsync(
            "GetShortDramaLeaderboardAsync",
            () =>
                client.GetShortDramaLeaderboardAsync(
                    category: ShortDramaLeaderboardCategory.Human,
                    board: ShortDramaLeaderboardBoard.HotPlay,
                    offset: 0,
                    cancellationToken: cancellationToken
                )
        );
        PrintCount("  排行榜剧集数量", leaderboard?.Items.Count);
        if (leaderboard is not null)
        {
            Console.WriteLine($"  ★ 榜单名称: {leaderboard.BoardTitle}");
            int rank = 1;
            foreach (Drama drama in leaderboard.Items.Take(5))
            {
                Console.WriteLine(
                    $"     TOP {rank++}: 《{drama.Title}》 - {drama.Remark} (评分: {drama.Score}, 热度: {drama.Heat})"
                );
            }
        }

        // ==========================================
        // 功能展示 4: 上新日历 (指定日期查询)
        // ==========================================
        Console.WriteLine("\n--- [功能展示 4: 上新日历 (今日与指定日期上线作品)] ---");
        DailyReleasePage? dailyReleases = await TryAsync(
            "GetDailyReleasesAsync",
            () =>
                client.GetDailyReleasesAsync(
                    targetDate: null,
                    offset: 0,
                    cancellationToken: cancellationToken
                )
        );
        PrintCount("  今日上线与预告剧集数", dailyReleases?.Items.Count);
        if (dailyReleases is not null)
        {
            Console.WriteLine($"  📅 当前日历日期: {dailyReleases.Date}");
            foreach (DailyReleaseItem item in dailyReleases.Items.Take(3))
            {
                Console.WriteLine(
                    $"     - 《{item.Drama.Title}》 - {item.Note} (已上线: {item.IsOnline})"
                );
            }
        }

        // ==========================================
        // 功能展示 5: App 首页发现频道流式瀑布流 (DiscoverPage)
        // ==========================================
        Console.WriteLine("\n--- [功能展示 5: 首页发现频道流式瀑布流 (DiscoverPage)] ---");
        DiscoverPage? discoverPage = await TryAsync(
            "GetDiscoverPageAsync",
            () =>
                client.GetDiscoverPageAsync(
                    DiscoverChannel.Recommend,
                    cursor: null,
                    cancellationToken: cancellationToken
                )
        );
        PrintCount("  推荐频道发现短剧数量", discoverPage?.Dramas.Count);
        PrintCount("  推荐频道原著小说数量", discoverPage?.Novels.Count);
        if (discoverPage?.Dramas.Count > 0)
        {
            foreach (Drama drama in discoverPage.Dramas.Take(3))
            {
                Console.WriteLine(
                    $"     - 《{drama.Title}》 ({drama.EpisodeCount}集) - 标签: {string.Join('/', drama.Tags)}"
                );
            }
        }

        // ==========================================
        // 功能展示 6: 多季合集与单剧专属关联推荐 (以选中剧集为例)
        // ==========================================
        Drama? sampleDrama = context.Search?.Dramas.FirstOrDefault();
        if (sampleDrama is not null && !string.IsNullOrWhiteSpace(sampleDrama.Id))
        {
            Console.WriteLine(
                $"\n--- [功能展示 6: 《{sampleDrama.Title}》多季合集与关联好剧推荐] ---"
            );
            DramaAlbum? album = await TryAsync(
                "GetDramaSeasonsAsync",
                () => client.GetDramaSeasonsAsync(sampleDrama.Id, cancellationToken)
            );
            if (album is not null)
            {
                Console.WriteLine($"  ★ 合集: 《{album.Title}》 (总季数: {album.Seasons.Count})");
                foreach (DramaSeason season in album.Seasons)
                {
                    Console.WriteLine(
                        $"     - 第 {season.Index} 季: 《{season.Title}》 (ID: {season.SeriesId}, {season.EpisodeCount}集)"
                    );
                }
            }

            DramaRelatedRecommendations? related = await TryAsync(
                "GetSeriesRecommendationsAsync",
                () => client.GetSeriesRecommendationsAsync(sampleDrama.Id, cancellationToken)
            );
            PrintCount("  单剧专属关联推荐短剧数", related?.Items.Count);
            if (related?.Items.Count > 0)
            {
                foreach (Drama drama in related.Items.Take(3))
                {
                    Console.WriteLine(
                        $"     - 推荐: 《{drama.Title}》 ({drama.EpisodeCount}集) - 评分: {drama.Score}"
                    );
                }
            }
        }

        // ==========================================
        // 功能展示 7: 跨模态搜索 (原著小说与明星创作者)
        // ==========================================
        Console.WriteLine("\n--- [功能展示 7: 跨模态搜索 (小说与演职员)] ---");
        SearchResult? novelSearch = await TryAsync(
            "SearchAsync(Novel)",
            () =>
                client.SearchAsync(
                    new SearchQuery
                    {
                        Keyword = "逆袭",
                        Tab = SearchTab.CommonBook,
                        Count = 3,
                    },
                    cancellationToken
                )
        );
        PrintCount("  搜索原著小说结果数", novelSearch?.Novels.Count);
        if (novelSearch?.Novels.Count > 0)
        {
            foreach (Novel novel in novelSearch.Novels.Take(3))
            {
                Console.WriteLine(
                    $"     - 书籍: 《{novel.Title}》 作者: {novel.Author} (字数/连载: {novel.StatusText})"
                );
            }
        }

        SearchResult? userSearch = await TryAsync(
            "SearchAsync(User)",
            () =>
                client.SearchAsync(
                    new SearchQuery
                    {
                        Keyword = "张",
                        Tab = SearchTab.User,
                        Count = 3,
                    },
                    cancellationToken
                )
        );
        PrintCount("  搜索明星/创作者结果数", userSearch?.Users.Count);
        if (userSearch?.Users.Count > 0)
        {
            foreach (User user in userSearch.Users.Take(3))
            {
                Console.WriteLine(
                    $"     - 用户: {user.Name} (ID: {user.Id}) - 简介: {user.Introduction}"
                );
            }
        }
    }
}
