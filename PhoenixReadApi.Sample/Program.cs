using PhoenixReadApi;

const string keyword = "请君入我怀";

using PhoenixReadApiClient client = new(
    new PhoenixReadApiOptions
    {
        MaxPagesPerCategory = 1,
        RequestTimeout = TimeSpan.FromSeconds(25),
        RetryCount = 2,
        CompleteSearchSeasons = false,
    }
);
using CancellationTokenSource timeout = new(TimeSpan.FromMinutes(4));

// Exercise every public request method. All IDs below come from the search,
// detail, and comment results for the keyword above.
IReadOnlyList<Drama>? catalog = await TryAsync(
    "GetCatalogAsync",
    () => client.GetCatalogAsync(PhoenixReadApiCategory.AiDrama, timeout.Token)
);
if (catalog is not null)
{
    Console.WriteLine($"  条目：{catalog.Count}");
}

IReadOnlyList<SearchSuggestion>? suggestions = await TryAsync(
    "GetSuggestionsAsync",
    () => client.GetSuggestionsAsync(keyword, maxResults: 5, cancellationToken: timeout.Token)
);
if (suggestions is not null)
{
    foreach (SearchSuggestion suggestion in suggestions)
    {
        Console.WriteLine($"  {suggestion.Name} | {suggestion.Type}");
    }
}

SearchResult? search = await TryAsync(
    "SearchAsync",
    () => client.SearchAsync(keyword, timeout.Token)
);
if (search is not null)
{
    Console.WriteLine($"  总数：{search.Total}，返回：{search.Items.Count}");
}

RecommendationPage? recommendations = await TryAsync(
    "GetRecommendationsAsync(query)",
    () =>
        client.GetRecommendationsAsync(
            new RecommendationQuery { Genre = RecommendationGenre.ShortDrama },
            timeout.Token
        )
);
if (recommendations is not null)
{
    Console.WriteLine(
        $"  条目：{recommendations.Items.Count}，下一页：{recommendations.NextOffset}"
    );
}

RecommendationPage? recommendationOverload = await TryAsync(
    "GetRecommendationsAsync(overload)",
    () =>
        client.GetRecommendationsAsync(
            RecommendationGenre.ShortDrama,
            cancellationToken: timeout.Token
        )
);
if (recommendationOverload is not null)
{
    Console.WriteLine($"  条目：{recommendationOverload.Items.Count}");
}

Drama? selectedDrama =
    search?.Items.FirstOrDefault(item =>
        string.Equals(item.Title, keyword, StringComparison.OrdinalIgnoreCase)
    )
    ?? search?.Items.FirstOrDefault(item =>
        item.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase)
    );
if (selectedDrama is null || selectedDrama.Id.Length == 0)
{
    Console.WriteLine($"  未找到与“{keyword}”匹配的剧集。");
    return;
}

DramaDetails? details = await TryAsync(
    "GetDetailsAsync",
    () => client.GetDetailsAsync(selectedDrama.Id, timeout.Token)
);
if (details is null)
{
    return;
}

string seriesId = details.Drama.Id;
Episode? firstEpisode = details.Episodes.FirstOrDefault();
if (firstEpisode is null || firstEpisode.VideoId.Length == 0)
{
    Console.WriteLine("  详情没有返回可用分集。");
    return;
}

string videoId = firstEpisode.VideoId;
Console.WriteLine($"  剧集：{details.Drama.Title}（{seriesId}），分集：{details.Episodes.Count}");
Console.WriteLine($"  首集：第{firstEpisode.Number}集（{videoId}）");
Console.WriteLine($"  署名账号：{details.Drama.Credits.Count}");
foreach (DramaCredit credit in details.Drama.Credits.Take(6))
{
    string roles = string.Join(
        "、",
        credit.Roles.Select(static role =>
            role.CharacterName.Length > 0 ? $"{role.Label}（{role.Type}）" : role.Label
        )
    );
    Console.WriteLine($"    {credit.User.Name} | {roles} | ID {credit.User.Id}");
}

if (details.Drama.Credits.Count > 0)
{
    UserProfile? profile = await TryAsync(
        "GetUserProfileAsync",
        () => client.GetUserProfileAsync(details.Drama.Credits[0], timeout.Token)
    );
    if (profile is not null)
    {
        Console.WriteLine(
            $"  账号主页：{profile.User.Name}，相关剧集：{profile.RelatedDramas.Count}，动态：{profile.Activities.Count}"
        );
    }

    string creditUserId = details.Drama.Credits[0].User.Id;
    if (creditUserId.Length > 0)
    {
        UserProfile? profileById = await TryAsync(
            "GetUserProfileAsync(userId)",
            () => client.GetUserProfileAsync(creditUserId, timeout.Token)
        );
        if (profileById is not null)
        {
            Console.WriteLine(
                $"  账号主页（UserId）：{profileById.User.Id}，公开剧集：{profileById.RelatedDramas.Count}"
            );
        }

        UserActivityPage? activityPage = await TryAsync(
            "GetUserActivitiesAsync(page)",
            () =>
                client.GetUserActivitiesAsync(
                    creditUserId,
                    count: 18,
                    cancellationToken: timeout.Token
                )
        );
        if (activityPage is not null)
        {
            Console.WriteLine(
                $"  动态分页：{activityPage.Items.Count}/{activityPage.Total}，还有下一页：{activityPage.HasMore}"
            );
            if (activityPage.HasMore && activityPage.Cursor.Length > 0)
            {
                UserActivityPage? nextActivityPage = await TryAsync(
                    "GetUserActivitiesAsync(next page)",
                    () =>
                        client.GetUserActivitiesAsync(
                            creditUserId,
                            count: 18,
                            cursor: activityPage.Cursor,
                            cancellationToken: timeout.Token
                        )
                );
                if (nextActivityPage is not null)
                {
                    Console.WriteLine(
                        $"  动态下一页：{nextActivityPage.Items.Count}/{nextActivityPage.Total}，还有下一页：{nextActivityPage.HasMore}"
                    );
                }
            }
        }
    }
}

CommentPage? reviews = await TryAsync(
    "GetDramaReviewsAsync",
    () => client.GetDramaReviewsAsync(seriesId, count: 5, cancellationToken: timeout.Token)
);
if (reviews is not null)
{
    Console.WriteLine(
        $"  剧评：{reviews.Items.Count}/{reviews.Total}，还有下一页：{reviews.HasMore}"
    );
    if (reviews.Items.Count > 0)
    {
        Console.WriteLine(
            $"    首条：{reviews.Items[0].Text[..Math.Min(40, reviews.Items[0].Text.Length)]}"
        );
    }
}

CommentPage? episodeComments = await TryAsync(
    "GetEpisodeCommentsAsync",
    () =>
        client.GetEpisodeCommentsAsync(
            seriesId,
            videoId,
            count: 5,
            cancellationToken: timeout.Token
        )
);
if (episodeComments is not null)
{
    Console.WriteLine(
        $"  分集评论：{episodeComments.Items.Count}/{episodeComments.Total}，还有下一页：{episodeComments.HasMore}"
    );
    if (episodeComments.Items.Count > 0)
    {
        Console.WriteLine(
            $"    首条：{episodeComments.Items[0].Text[..Math.Min(40, episodeComments.Items[0].Text.Length)]}"
        );
    }

    HashSet<string> creditUserIds = details
        .Drama.Credits.Select(static credit => credit.User.Id)
        .Where(static id => id.Length > 0)
        .ToHashSet(StringComparer.Ordinal);
    string ordinaryUserId =
        episodeComments
            .Items.Select(static comment => comment.Author.Id)
            .FirstOrDefault(id => id.Length > 0 && !creditUserIds.Contains(id))
        ?? string.Empty;
    if (ordinaryUserId.Length > 0)
    {
        UserProfile? ordinaryProfile = await TryAsync(
            "GetUserProfileAsync(ordinary user)",
            () => client.GetUserProfileAsync(ordinaryUserId, timeout.Token)
        );
        if (ordinaryProfile is not null)
        {
            Console.WriteLine(
                $"  普通账号内容：收藏 {ordinaryProfile.Collections.Count}/{ordinaryProfile.CollectionsPage.Total}，点赞 {ordinaryProfile.Likes.Count}/{ordinaryProfile.LikesPage.Total}，动态 {ordinaryProfile.Activities.Count}/{ordinaryProfile.ActivitiesPage.Total}，相关剧集 {ordinaryProfile.RelatedDramas.Count}/{ordinaryProfile.RelatedDramasPage.Total}"
            );
        }

        UserDramaPage? collectionsPage = await TryAsync(
            "GetUserCollectionsAsync(page)",
            () =>
                client.GetUserCollectionsAsync(
                    ordinaryUserId,
                    count: 18,
                    cancellationToken: timeout.Token
                )
        );
        if (collectionsPage is not null)
        {
            Console.WriteLine(
                $"  收藏分页：{collectionsPage.Items.Count}/{collectionsPage.Total}，下一偏移：{collectionsPage.NextOffset}，还有下一页：{collectionsPage.HasMore}"
            );
        }

        UserDramaPage? likesPage = await TryAsync(
            "GetUserLikesAsync(page)",
            () =>
                client.GetUserLikesAsync(
                    ordinaryUserId,
                    count: 18,
                    cancellationToken: timeout.Token
                )
        );
        if (likesPage is not null)
        {
            Console.WriteLine(
                $"  点赞分页：{likesPage.Items.Count}/{likesPage.Total}，下一偏移：{likesPage.NextOffset}，还有下一页：{likesPage.HasMore}"
            );
        }
    }
    else
    {
        Console.WriteLine("  分集评论中没有返回可用的普通账号 ID。");
    }
}

PlaybackMedia? mediaByIds = await TryAsync(
    "ResolveMediaAsync(ids)",
    () => client.ResolveMediaAsync(seriesId, videoId, timeout.Token)
);
if (mediaByIds is not null)
{
    Console.WriteLine(
        $"  地址：{mediaByIds.Url} | 清晰度：{mediaByIds.Quality}p | 变体：{mediaByIds.Variants.Count} | CENC 密钥：{(mediaByIds.DecryptionKey.Length > 0 ? "有" : "无")}"
    );
}

PlaybackMedia? mediaByChapter = await TryAsync(
    "ResolveMediaAsync(episode)",
    () => client.ResolveMediaAsync(seriesId, firstEpisode, timeout.Token)
);
if (mediaByChapter is not null)
{
    Console.WriteLine(
        $"  地址：{mediaByChapter.Url} | 清晰度：{mediaByChapter.Quality}p | 变体：{mediaByChapter.Variants.Count} | CENC 密钥：{(mediaByChapter.DecryptionKey.Length > 0 ? "有" : "无")}"
    );
}

var danmakuDurationMs =
    mediaByIds?.Duration > TimeSpan.Zero ? (long)Math.Ceiling(mediaByIds.Duration.TotalMilliseconds)
    : mediaByChapter?.Duration > TimeSpan.Zero
        ? (long)Math.Ceiling(mediaByChapter.Duration.TotalMilliseconds)
    : 60_000L;
DanmakuPage? danmaku = await TryAsync(
    "GetDanmakuAsync",
    () => client.GetDanmakuAsync(seriesId, videoId, 0, danmakuDurationMs, timeout.Token)
);
if (danmaku is not null)
{
    Console.WriteLine($"  弹幕：{danmaku.Items.Count}/{danmaku.Total}，已读取至：{danmaku.NextMs}");
}

static async Task<T?> TryAsync<T>(string name, Func<Task<T>> operation)
{
    Console.WriteLine($"{name}:");
    try
    {
        return await operation();
    }
    catch (PhoenixReadApiException exception)
    {
        Console.WriteLine(
            $"  失败：{exception.Message}（端点：{exception.Endpoint ?? "unknown"}）"
        );
        return default;
    }
    catch (Exception exception)
    {
        Console.WriteLine($"  未处理错误：{exception.Message}");
        return default;
    }
}
