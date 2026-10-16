using PhoenixReadApi;

const string seriesId = "7687438098002611225";
const string videoId = "7687440021728529432";

using PhoenixReadApiClient client = new(
    new PhoenixReadApiOptions
    {
        MaxPagesPerCategory = 1,
        RequestTimeout = TimeSpan.FromSeconds(25),
        RetryCount = 1,
        CompleteSearchSeasons = false,
    }
);
using CancellationTokenSource timeout = new(TimeSpan.FromMinutes(4));

// Exercise every public request method. Playback may be rejected for a
// protected episode, so each operation is reported independently.
IReadOnlyList<Drama>? catalog = await TryAsync(
    "GetCatalogAsync",
    () => client.GetCatalogAsync(PhoenixReadApiCategory.RealDrama, timeout.Token)
);
if (catalog is not null)
{
    Console.WriteLine($"  条目：{catalog.Count}");
}

IReadOnlyList<SearchSuggestion>? suggestions = await TryAsync(
    "GetSuggestionsAsync",
    () => client.GetSuggestionsAsync("世子很凶", maxResults: 5, cancellationToken: timeout.Token)
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
    () => client.SearchAsync("世子很凶", timeout.Token)
);
if (search is not null)
{
    Console.WriteLine($"  总数：{search.Total}，返回：{search.Items.Count}");
}

RecommendationPage? recommendations = await TryAsync(
    "GetRecommendationsAsync(query)",
    () =>
        client.GetRecommendationsAsync(
            new RecommendationQuery { Genre = "short_play" },
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
    () => client.GetRecommendationsAsync("short_play", cancellationToken: timeout.Token)
);
if (recommendationOverload is not null)
{
    Console.WriteLine($"  条目：{recommendationOverload.Items.Count}");
}

DramaDetails? details = await TryAsync(
    "GetDetailsAsync",
    () => client.GetDetailsAsync(seriesId, timeout.Token)
);
if (details is not null)
{
    Console.WriteLine($"  剧集：{details.Drama.Title}，分集：{details.Chapters.Count}");
    Console.WriteLine($"  演员：{details.Drama.Actors.Count}");
    foreach (DramaActor actor in details.Drama.Actors.Take(3))
    {
        Console.WriteLine($"    {actor.Name} | 饰 {actor.RoleName} | ID {actor.Id} | Profile {actor.ProfileId}");
    }

    if (details.Drama.Actors.Count > 0)
    {
        Actor? actor = await TryAsync(
            "GetActorAsync",
            () => client.GetActorAsync(details.Drama.Actors[0], timeout.Token)
        );
        if (actor is not null)
        {
            Console.WriteLine($"  演员主页：{actor.Name}，参演剧集：{actor.Works.Count}");
        }

        if (details.Drama.Actors[0].ProfileId.Length > 0)
        {
            Actor? actorByProfileId = await TryAsync(
                "GetActorAsync(profileId)",
                () => client.GetActorAsync(details.Drama.Actors[0].ProfileId, timeout.Token)
            );
            if (actorByProfileId is not null)
            {
                Console.WriteLine(
                    $"  演员主页（ProfileId）：{actorByProfileId.ProfileId}，参演剧集：{actorByProfileId.Works.Count}"
                );
            }
        }
    }
}

CommentPage? reviews = await TryAsync(
    "GetReviewsAsync",
    () => client.GetReviewsAsync(seriesId, count: 5, cancellationToken: timeout.Token)
);
if (reviews is not null)
{
    Console.WriteLine($"  剧评：{reviews.Items.Count}/{reviews.Total}，还有下一页：{reviews.HasMore}");
    if (reviews.Items.Count > 0)
    {
        Console.WriteLine($"    首条：{reviews.Items[0].Text[..Math.Min(40, reviews.Items[0].Text.Length)]}");
    }
}

CommentPage? episodeComments = await TryAsync(
    "GetEpisodeCommentsAsync",
    () => client.GetEpisodeCommentsAsync(seriesId, videoId, count: 5, cancellationToken: timeout.Token)
);
if (episodeComments is not null)
{
    Console.WriteLine($"  分集评论：{episodeComments.Items.Count}/{episodeComments.Total}，还有下一页：{episodeComments.HasMore}");
    if (episodeComments.Items.Count > 0)
    {
        Console.WriteLine($"    首条：{episodeComments.Items[0].Text[..Math.Min(40, episodeComments.Items[0].Text.Length)]}");
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
    "ResolveMediaAsync(chapter)",
    () => client.ResolveMediaAsync(new Chapter { VideoId = videoId }, seriesId, timeout.Token)
);
if (mediaByChapter is not null)
{
    Console.WriteLine(
        $"  地址：{mediaByChapter.Url} | 清晰度：{mediaByChapter.Quality}p | 变体：{mediaByChapter.Variants.Count} | CENC 密钥：{(mediaByChapter.DecryptionKey.Length > 0 ? "有" : "无")}"
    );
}

DanmakuPage? danmaku = await TryAsync(
    "GetDanmakuAsync",
    () => client.GetDanmakuAsync(seriesId, videoId, 0, 60_000, timeout.Token)
);
if (danmaku is not null)
{
    Console.WriteLine($"  弹幕：{danmaku.Items.Count}/{danmaku.Total}，下一时间：{danmaku.NextMs}");
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
