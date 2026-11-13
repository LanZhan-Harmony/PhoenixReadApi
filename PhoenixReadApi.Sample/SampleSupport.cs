using System.Diagnostics;

namespace PhoenixReadApi.Sample;

internal sealed class SampleContext
{
    public required PhoenixReadApiClient Client { get; init; }
    public required string Keyword { get; init; }
    public required CancellationToken CancellationToken { get; init; }

    public AuthenticationState Authentication { get; set; } = null!;
    public IReadOnlyList<Drama>? Catalog { get; set; }
    public SearchResult? Search { get; set; }

    public string SeriesId { get; set; } = string.Empty;
    public string VideoId { get; set; } = string.Empty;
    public Episode? FirstEpisode { get; set; }
    public string CreditUserId { get; set; } = string.Empty;
    public string ProfileUserId { get; set; } = string.Empty;
    public DramaDetails? Details { get; set; }
    public IReadOnlyList<PlaybackMedia>? Medias { get; set; }
    public PlaybackMedia? Media => Medias?.FirstOrDefault();
    public DanmakuPage? Danmaku { get; set; }
    public CommentPage? Reviews { get; set; }
    public CommentPage? EpisodeComments { get; set; }
    public DramaComment? SampleComment { get; set; }
    public CommentReplyPage? Replies { get; set; }
    public DramaPage? Favorites { get; set; }
    public DramaPage? Likes { get; set; }
    public ReadHistoryPage? History { get; set; }
    public ReservationPage? UpcomingReservations { get; set; }

    public static string GetSampleDirectory()
    {
        string candidate = Path.Combine(AppContext.BaseDirectory, "..", "..", "..");
        return Directory.Exists(candidate) ? candidate : AppContext.BaseDirectory;
    }

    public static string ReadPhone(string sampleDirectory)
    {
        string path = Path.Combine(sampleDirectory, ".auth", "Phone.txt");
        return File.Exists(path) ? File.ReadAllText(path).Trim() : string.Empty;
    }

    public Drama? FindDrama()
    {
        return Search?.Dramas.FirstOrDefault(item =>
                string.Equals(item.Title, Keyword, StringComparison.OrdinalIgnoreCase)
            )
            ?? Search?.Dramas.FirstOrDefault(item =>
                item.Title.Contains(Keyword, StringComparison.OrdinalIgnoreCase)
            )
            ?? Search?.Dramas.FirstOrDefault()
            ?? Catalog?.FirstOrDefault(item =>
                item.Title.Contains(Keyword, StringComparison.OrdinalIgnoreCase)
            )
            ?? Catalog?.FirstOrDefault();
    }

    public string FindOrdinaryUserId()
    {
        HashSet<string> creditIds =
            Details
                ?.Drama.Credits.Select(static credit => credit.User.Id)
                .Where(static id => id.Length > 0)
                .ToHashSet(StringComparer.Ordinal)
            ?? [];
        return EpisodeComments
                ?.Items.Select(static comment => comment.Author.Id)
                .FirstOrDefault(id => id.Length > 0 && !creditIds.Contains(id))
            ?? string.Empty;
    }
}

internal static partial class SampleSteps
{
    private static async Task<T?> TryAsync<T>(string name, Func<Task<T>> operation)
        where T : class
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        Console.WriteLine($"[RUN] {name}");
        try
        {
            T result = await operation();
            Console.WriteLine($"[ OK] {name} ({stopwatch.ElapsedMilliseconds} ms)");
            return result;
        }
        catch (PhoenixReadApiException exception)
        {
            Console.WriteLine(
                $"[ERR] {name}: {exception.Message} | {exception.Endpoint ?? "unknown"}"
            );
        }
        catch (Exception exception)
        {
            Console.WriteLine($"[ERR] {name}: {exception.GetType().Name}: {exception.Message}");
        }
        return null;
    }

    private static async Task<T?> TryWhenIdAsync<T>(
        string name,
        string id,
        Func<string, Task<T>> operation
    )
        where T : class
    {
        return id.Length == 0 ? null : await TryAsync(name, () => operation(id));
    }

    private static async Task<T?> TryWhenIdsAsync<T>(
        string name,
        string firstId,
        string secondId,
        Func<string, string, Task<T>> operation
    )
        where T : class
    {
        return firstId.Length == 0 || secondId.Length == 0
            ? null
            : await TryAsync(name, () => operation(firstId, secondId));
    }

    private static async Task<(IReadOnlyList<string> Ids, DramaPage Page)?> TryFavoriteIdsAsync(
        string name,
        Func<Task<(IReadOnlyList<string> Ids, DramaPage Page)>> operation
    )
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        Console.WriteLine($"[RUN] {name}");
        try
        {
            (IReadOnlyList<string> Ids, DramaPage Page) result = await operation();
            Console.WriteLine($"[ OK] {name} ({stopwatch.ElapsedMilliseconds} ms)");
            return result;
        }
        catch (PhoenixReadApiException exception)
        {
            Console.WriteLine(
                $"[ERR] {name}: {exception.Message} | {exception.Endpoint ?? "unknown"}"
            );
        }
        catch (Exception exception)
        {
            Console.WriteLine($"[ERR] {name}: {exception.GetType().Name}: {exception.Message}");
        }
        return null;
    }

    private static T? RunSync<T>(string name, Func<T> operation)
        where T : class
    {
        Console.WriteLine($"[RUN] {name}");
        try
        {
            T result = operation();
            Console.WriteLine($"[ OK] {name}");
            return result;
        }
        catch (Exception exception)
        {
            Console.WriteLine($"[ERR] {name}: {exception.GetType().Name}: {exception.Message}");
            return default!;
        }
    }

    private static void PrintCount(string label, int? count)
    {
        if (count is not null)
        {
            Console.WriteLine($"{label}：{count}");
        }
    }

    private static void PrintMedia(IReadOnlyList<PlaybackMedia>? medias)
    {
        if (medias is { Count: > 0 })
        {
            PlaybackMedia best = medias[0];
            Console.WriteLine(
                $"  播放媒体数：{medias.Count} | 推荐最佳画质：{best.Definition} ({best.QualityName}, {best.Codec}) | key={(best.DecryptionKey.Length > 0 ? "yes" : "no")}"
            );
            foreach (PlaybackMedia m in medias)
            {
                Console.WriteLine(
                    $"    - [{m.Definition}] {m.QualityName} ({m.Codec}) {m.Width}x{m.Height} 码率:{m.Bitrate}b/s 大小:{m.Size}B 直链:{m.Url[..Math.Min(40, m.Url.Length)]}..."
                );
            }
        }
    }

    private static void PrintComments(string label, CommentPage? page)
    {
        if (page is not null)
        {
            int imageCommentCount = page.Items.Count(static c => c.Images.Count > 0);
            Console.WriteLine(
                $"{label}：{page.Items.Count}/{page.Total}，HasMore={page.HasMore}，含附图评论数={imageCommentCount}"
            );
        }
    }

    private static void PrintProfile(UserProfile? profile)
    {
        if (profile is not null)
        {
            Console.WriteLine(
                $"  用户：{profile.User.Id}/{profile.User.Name} | collections={profile.Collections.Count} likes={profile.Likes.Count} activities={profile.Activities.Count} works={profile.Works.Count}"
            );
        }
    }
}
