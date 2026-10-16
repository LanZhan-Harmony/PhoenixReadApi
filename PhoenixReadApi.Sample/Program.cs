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
}

PlaybackMedia? mediaByIds = await TryAsync(
    "ResolveMediaAsync(ids)",
    () => client.ResolveMediaAsync(seriesId, videoId, timeout.Token)
);
if (mediaByIds is not null)
{
    Console.WriteLine($"  地址：{mediaByIds.Url}");
}

PlaybackMedia? mediaByChapter = await TryAsync(
    "ResolveMediaAsync(chapter)",
    () => client.ResolveMediaAsync(new Chapter { VideoId = videoId }, seriesId, timeout.Token)
);
if (mediaByChapter is not null)
{
    Console.WriteLine($"  地址：{mediaByChapter.Url}");
}

DanmakuPage? danmaku = await TryAsync(
    "GetDanmakuAsync",
    () => client.GetDanmakuAsync(seriesId, videoId, 0, 600_000, timeout.Token)
);
if (danmaku is not null)
{
    Console.WriteLine($"  弹幕：{danmaku.Items.Count}，下一时间：{danmaku.NextMs}");
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
