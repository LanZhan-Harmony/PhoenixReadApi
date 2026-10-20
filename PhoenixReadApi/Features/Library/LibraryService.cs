using PhoenixReadApi.Internal.Contracts.App;
using PhoenixReadApi.Internal.Contracts.App.Comment;
using PhoenixReadApi.Internal.Contracts.App.Library;
using PhoenixReadApi.Internal.Contracts.Shared.Drama;
using PhoenixReadApi.Internal.Features.Shared;
using PhoenixReadApi.Internal.Infrastructure.Runtime;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Features.Library;

/// <summary>阅读历史、进度、收藏、点赞、预约和分类等 App 接口。</summary>
internal sealed partial class LibraryService : PhoenixFeatureService
{
    private const int MaxPageSize = 50;

    internal LibraryService(PhoenixReadClientRuntime runtime)
        : base(runtime) { }

    private static ReadHistoryEntry MapHistory(AppHistoryItem item)
    {
        AppHistoryVideoData video = item.Video ?? new();
        return new ReadHistoryEntry
        {
            Id = item.Id,
            SeriesId = ValueNormalizer.First(
                item.BookIdString,
                item.SeriesId,
                item.BookId,
                video.SeriesId
            ),
            VideoId = ValueNormalizer.First(item.VideoId, item.VideoIdAlias, video.VideoId),
            EpisodeNumber = item.EpisodeNumber > 0 ? item.EpisodeNumber : video.EpisodeNumber,
            Title = ValueNormalizer.First(item.BookName, item.Title, video.Title),
            CoverUrl = ValueNormalizer.First(item.ThumbUrl, item.CoverUrl, video.CoverUrl),
            CurrentPositionMs = Math.Max(item.CurrentPositionMs, item.CurrentPositionAlias),
            DurationMs = Math.Max(0, item.DurationMs),
            UpdatedAtUnixMilliseconds = Math.Max(
                Math.Max(0, item.UpdatedAtUnixMilliseconds),
                Math.Max(0, item.ReadTimestampMilliseconds)
            ),
        };
    }

    private static ReadProgressPage MapProgress(
        IReadOnlyList<AppReadProgressItem>? items,
        IReadOnlyList<AppReadProgressItem>? localItems
    )
    {
        items ??= [];
        localItems ??= [];
        return new ReadProgressPage
        {
            Items = [.. items.Select(MapProgress)],
            LocalItems = [.. localItems.Select(MapProgress)],
        };
    }

    private static ReadProgressPage MapProgress(AppReadProgressResponse response)
    {
        IReadOnlyList<AppReadProgressItem> items = response.VideoData;
        IReadOnlyList<AppReadProgressItem> localItems = response.LocalBookData;
        if (items.Count == 0 && localItems.Count == 0 && response.Data is not null)
        {
            items = response.Data.Items;
            localItems = response.Data.LocalItems;
            if (items.Count == 0 && localItems.Count == 0 && response.Data.DataItems.Count > 0)
            {
                items = response.Data.DataItems;
            }
        }
        return MapProgress(items, localItems);
    }

    private static ReadProgress MapProgress(AppReadProgressItem item) =>
        new()
        {
            SeriesId = item.SeriesId,
            VideoId = item.VideoId,
            EpisodeNumber = item.EpisodeNumber,
            EpisodeCount = item.EpisodeCount,
            LastWatchTimeUnixMilliseconds = item.LastWatchTime,
            CurrentPositionMs = item.CurrentPlayTime,
            DurationMs = item.CurrentTotalTime,
            CumulativeDurationMs = item.CumulativeDuration,
        };

    private static Drama MapShelf(AppShelfItem item)
    {
        AppShelfVideoData video = item.Video ?? new();
        string seriesId = ValueNormalizer.First(item.SeriesId, video.SeriesId);
        // The real bookshelf list currently returns only series_id for short dramas.
        // Keep missing metadata empty instead of presenting the ID as a title; callers
        // can request full details with GetDramaDetailsAsync(seriesId).
        return new Drama
        {
            Id = seriesId,
            Title = item.Title,
            Description = item.Description,
            CoverUrl = item.CoverUrl,
            EpisodeCount = video.EpisodeCount,
        };
    }

    private static Drama MapLike(AppUserActionRecord item)
    {
        AppUserActionVideoData video = item.Video ?? new();
        AppUserActionSeriesDetail series = video.Series ?? new();
        string id = ValueNormalizer.First(series.SeriesId, video.SeriesId);
        return DramaMapper.FromAction(
            id,
            series.Title,
            ValueNormalizer.First(series.Introduction, video.Description),
            ValueNormalizer.First(series.CoverUrl, video.CoverUrl),
            Math.Max(series.EpisodeCount, video.EpisodeCount),
            series.Status,
            series.Tags
        );
    }

    private static FollowUpdate MapFollow(AppFollowItem item)
    {
        AppFollowVideo video = item.Video ?? new();
        return new FollowUpdate
        {
            Id = ValueNormalizer.First(item.Id, video.SeriesId),
            UserId = item.UserId,
            Text = ValueNormalizer.First(item.Text, video.Title),
            CoverUrl = ValueNormalizer.First(item.CoverUrl, video.CoverUrl),
            CreatedAtUnixSeconds = Math.Max(0, item.CreatedAt),
            Drama = DramaMapper.FromRelatedDrama(
                video.SeriesId,
                video.Title,
                string.Empty,
                video.CoverUrl,
                0
            ),
        };
    }

    private static ReservationPage MapReservations(AppReservationData data)
    {
        IReadOnlyList<AppReservationItem> items =
            data.Items is { Count: > 0 } ? data.Items
            : data.SubscribeItems is { Count: > 0 } ? data.SubscribeItems
            : data.LegacySubscribeItems ?? [];
        return new ReservationPage
        {
            Items = [.. items.Select(MapReservation)],
            NextOffset = data.NextOffset,
            HasMore = data.HasMore,
            Total = Math.Max(
                items.Count,
                Math.Max(data.Total, Math.Max(data.OnlineTotalCount, data.OfflineTotalCount))
            ),
        };
    }

    private static Reservation MapReservation(AppReservationItem item) =>
        new()
        {
            Id = ValueNormalizer.First(item.Id, item.ItemId),
            SeriesId = ValueNormalizer.First(item.SeriesId, item.ItemId),
            VideoId = item.VideoId,
            Title = ValueNormalizer.First(item.Title, item.Name),
            CoverUrl = item.CoverUrl,
            ReservedAtUnixSeconds = Math.Max(0, item.ReservedAt),
            ReleaseAtUnixSeconds = Math.Max(item.ReleaseAt, item.ExpectedPublishAt),
            IsActive = item.IsOnline || item.ReleaseAt > 0,
        };

    private static FeedPage MapFeed(
        IReadOnlyList<AppFeedCell>? cells,
        int nextOffset,
        string sessionId,
        bool hasMore
    ) =>
        new()
        {
            Sections = MapFeedSections(cells ?? [], ""),
            NextOffset = nextOffset,
            SessionId = sessionId ?? string.Empty,
            HasMore = hasMore,
        };

    private static IEnumerable<AppRelatedDramaItem> FlattenRelatedDramas(AppRelatedDramaItem item)
    {
        yield return item;
        foreach (AppRelatedDramaItem child in item.Children.SelectMany(FlattenRelatedDramas))
        {
            yield return child;
        }
    }

    private static IReadOnlyList<FeedSection> MapFeedSections(
        IReadOnlyList<AppFeedCell> cells,
        string parentTitle
    )
    {
        List<FeedSection> sections = [];
        foreach (AppFeedCell cell in cells ?? [])
        {
            IReadOnlyList<DramaDto> videoItems = cell.VideoItems ?? [];
            IReadOnlyList<DramaDto> bookItems = cell.BookItems ?? [];
            IReadOnlyList<DramaDto> rows = videoItems.Count > 0 ? videoItems : bookItems;
            List<Drama> items =
            [
                .. rows.Select(item => DramaMapper.FromDto(item, "推荐"))
                    .Where(static item => item.Id.Length > 0),
            ];
            string title = cell.Name.Length > 0 ? cell.Name : parentTitle;
            if (items.Count > 0)
            {
                sections.Add(
                    new FeedSection
                    {
                        Id = cell.Id,
                        Title = title,
                        Items = items,
                    }
                );
            }
            sections.AddRange(MapFeedSections(cell.Children ?? [], title));
        }
        return sections;
    }

    private static PhoenixOperationResult Succeeded(string message) =>
        new() { Succeeded = true, Message = message ?? string.Empty };

    private static void ValidatePage(int count, int offset)
    {
        ValidateCount(count);
        if (offset is < 0 or > 1_000_000)
        {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }
    }

    private static void ValidateCount(int count)
    {
        if (count is < 1 or > MaxPageSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(count),
                $"数量必须在 1 到 {MaxPageSize} 之间。"
            );
        }
    }

    private static string NormalizeCursor(string value)
    {
        value = (value ?? string.Empty).Trim();
        if (value.Length > 8192 || value.Any(static c => c is '\r' or '\n' or '\0'))
        {
            throw new ArgumentException("游标无效。", nameof(value));
        }
        return value;
    }

    private static Dictionary<string, string> ParseQuery(string query)
    {
        Dictionary<string, string> values = new(StringComparer.OrdinalIgnoreCase);
        foreach (
            string item in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries)
        )
        {
            string[] parts = item.Split('=', 2);
            if (parts.Length == 2)
            {
                values[Uri.UnescapeDataString(parts[0])] = Uri.UnescapeDataString(parts[1]);
            }
        }
        return values;
    }

    private static string FirstQuery(
        IReadOnlyDictionary<string, string> values,
        params string[] names
    )
    {
        foreach (string name in names)
        {
            if (values.TryGetValue(name, out string? value) && value.Length > 0)
            {
                return value;
            }
        }
        return string.Empty;
    }
}
