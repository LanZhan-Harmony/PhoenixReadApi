using PhoenixReadApi;
using PhoenixApi = PhoenixReadApi.PhoenixReadApi;

using PhoenixApi client = new(new PhoenixReadApiOptions { MaxPagesPerCategory = 1 });
using CancellationTokenSource cancellation = new(TimeSpan.FromSeconds(45));

try
{
    SearchResult result = await client.SearchAsync("世子很凶", cancellation.Token);
    Console.WriteLine($"搜索：{result.Total} 条，当前返回：{result.Items.Count} 条");
    if (!string.IsNullOrWhiteSpace(result.Warning))
    {
        Console.WriteLine("提示：" + result.Warning);
    }

    foreach (Drama drama in result.Items)
    {
        Console.WriteLine(
            $"{drama.SourceId} | {drama.DisplayTitle} | {drama.EpisodeCount} 集 | {drama.CoverUrl}"
        );
    }

    return;

    IReadOnlyList<SearchSuggestion> result2 = await client.GetSuggestionsAsync(
        "女帝",
        cancellation.Token
    );
    Console.WriteLine($"联想：{result2.Count} 条");
    foreach (SearchSuggestion suggestion in result2)
    {
        Console.WriteLine($"{suggestion.Name} | {suggestion.Type}");
    }

    return;

    PhoenixReadApiCategory category = PhoenixReadApiCategory.All;
    CatalogResult catalog = await client.GetCatalogResultAsync(category, cancellation.Token);
    Console.WriteLine(
        $"分类：{category}，条目：{catalog.Items.Count}，网页兜底：{catalog.UsedWebFallback}"
    );
    if (catalog.Warnings.Count > 0)
    {
        Console.WriteLine("提示：" + string.Join("；", catalog.Warnings));
    }

    foreach (Drama? drama in catalog.Items.Take(10))
    {
        Console.WriteLine(
            $"{drama.SourceId} | {drama.DisplayTitle} | {drama.EpisodeCount} 集 | {drama.CoverUrl}"
        );
    }

    if (catalog.Items.Count > 0)
    {
        DramaDetails details = await client.GetDetailsAsync(
            catalog.Items[0].SourceId,
            cancellation.Token
        );
        Console.WriteLine($"详情：{details.Drama.DisplayTitle}，分集：{details.Chapters.Count}");
        if (details.Chapters.Count > 0)
        {
            PlaybackMedia media = await client.ResolveMediaAsync(
                details.Drama.SourceId,
                details.Chapters[0].VideoUrl,
                cancellation.Token
            );
            Console.WriteLine($"播放地址：{media.Url}");
            Console.WriteLine(
                $"清晰度：{media.Quality}，变体：{media.Variants.Count}，CENC Key：{(media.DecryptionKey.Length == 0 ? "无" : "已提取")}"
            );
        }
    }
}
catch (OperationCanceledException)
{
    Console.Error.WriteLine("请求已取消或超时。");
    Environment.ExitCode = 2;
}
catch (PhoenixReadApiException exception)
{
    Console.Error.WriteLine($"PhoenixReadApi 失败：{exception.Message}");
    if (exception.Endpoint is not null)
    {
        Console.Error.WriteLine($"端点：{exception.Endpoint}");
    }

    if (exception.StatusCode is not null)
    {
        Console.Error.WriteLine($"HTTP：{exception.StatusCode}");
    }

    Environment.ExitCode = 1;
}
catch (Exception exception)
{
    Console.Error.WriteLine($"未处理错误：{exception.Message}");
    Environment.ExitCode = 1;
}
