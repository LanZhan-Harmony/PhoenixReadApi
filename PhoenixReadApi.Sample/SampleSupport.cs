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
    public PlaybackMedia? Media { get; set; }
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
        string candidate = Path.Combine(Environment.CurrentDirectory, "PhoenixReadApi.Sample");
        return Directory.Exists(candidate) ? candidate : AppContext.BaseDirectory;
    }

    public static string ReadPhone(string sampleDirectory)
    {
        string path = Path.Combine(sampleDirectory, ".auth", "Phone.txt");
        return File.Exists(path) ? File.ReadAllText(path).Trim() : string.Empty;
    }

    public Drama? FindDrama()
    {
        return Search?.Items.FirstOrDefault(item =>
                string.Equals(item.Title, Keyword, StringComparison.OrdinalIgnoreCase)
            )
            ?? Search?.Items.FirstOrDefault(item =>
                item.Title.Contains(Keyword, StringComparison.OrdinalIgnoreCase)
            )
            ?? Search?.Items.FirstOrDefault()
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

    private static void PrintMedia(PlaybackMedia? media)
    {
        if (media is not null)
        {
            Console.WriteLine(
                $"  播放：{media.Url} | {media.Quality}p | variants={media.Variants.Count} | key={(media.DecryptionKey.Length > 0 ? "yes" : "no")}"
            );
        }
    }

    private static void PrintComments(string label, CommentPage? page)
    {
        if (page is not null)
        {
            Console.WriteLine($"{label}：{page.Items.Count}/{page.Total}，HasMore={page.HasMore}");
        }
    }

    private static void PrintProfile(UserProfile? profile)
    {
        if (profile is not null)
        {
            Console.WriteLine(
                $"  用户：{profile.User.Id}/{profile.User.Name} | collections={profile.Collections.Count} likes={profile.Likes.Count} activities={profile.Activities.Count} related={profile.RelatedDramas.Count}"
            );
        }
    }
}
