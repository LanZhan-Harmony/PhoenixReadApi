using System.Globalization;
using PhoenixReadApi.Internal.Contracts.App;
using PhoenixReadApi.Internal.Contracts.App.Library;
using PhoenixReadApi.Internal.Infrastructure.Http;
using PhoenixReadApi.Internal.Infrastructure.Runtime;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Features.Library;

using Drama = PhoenixReadApi.Drama;
using User = PhoenixReadApi.User;

/// <summary>阅读历史及播放进度上报服务。</summary>
internal sealed class HistoryService : LibraryServiceBase
{
    internal HistoryService(PhoenixReadClientRuntime runtime)
        : base(runtime) { }

    internal Task<HistoryPage<Drama>> GetDramaHistoryAsync(
        int count,
        int offset,
        bool querySoftDeleted,
        long lastMinReadTimestampMilliseconds,
        CancellationToken cancellationToken
    ) =>
        FetchHistoryAsync(
            bookType: "2",
            count,
            offset,
            querySoftDeleted,
            lastMinReadTimestampMilliseconds,
            static (item, id, videoId, ep, title, cover) =>
                videoId.Length == 0 && id.Length == 0
                    ? null
                    : new Drama
                    {
                        Id = id,
                        Title = title,
                        CoverUrl = cover,
                        EpisodeCount = Math.Max(0, item.EpisodeCount),
                    },
            cancellationToken
        );

    internal Task<HistoryPage<DramaAlbum>> GetAlbumHistoryAsync(
        int count,
        int offset,
        bool querySoftDeleted,
        long lastMinReadTimestampMilliseconds,
        CancellationToken cancellationToken
    ) =>
        FetchHistoryAsync(
            bookType: "3",
            count,
            offset,
            querySoftDeleted,
            lastMinReadTimestampMilliseconds,
            static (item, id, _, _, title, cover) =>
                id.Length == 0
                    ? null
                    : new DramaAlbum
                    {
                        Id = id,
                        Title = title,
                        CoverUrl = cover,
                        SeasonCount = Math.Max(0, item.EpisodeCount),
                    },
            cancellationToken
        );

    internal Task<HistoryPage<ShortVideo>> GetShortVideoHistoryAsync(
        int count,
        int offset,
        bool querySoftDeleted,
        long lastMinReadTimestampMilliseconds,
        CancellationToken cancellationToken
    ) =>
        FetchHistoryAsync(
            bookType: "4",
            count,
            offset,
            querySoftDeleted,
            lastMinReadTimestampMilliseconds,
            static (item, id, videoId, _, title, cover) =>
            {
                string effectiveVid = ValueNormalizer.First(videoId, id);
                return effectiveVid.Length == 0
                    ? null
                    : new ShortVideo
                    {
                        Id = ValueNormalizer.First(id, effectiveVid),
                        VideoId = effectiveVid,
                        Title = title,
                        CoverUrl = cover,
                        Author = new User
                        {
                            Name = item.PugcUserName,
                            AvatarUrl = item.PugcUserAvatar,
                        },
                        LikeCount = Math.Max(0, item.DiggCount),
                        ViewCount = Math.Max(0, item.PlayCount),
                    };
            },
            cancellationToken
        );

    internal Task<HistoryPage<Post>> GetPostHistoryAsync(
        int count,
        int offset,
        bool querySoftDeleted,
        long lastMinReadTimestampMilliseconds,
        CancellationToken cancellationToken
    ) =>
        FetchHistoryAsync(
            bookType: "5",
            count,
            offset,
            querySoftDeleted,
            lastMinReadTimestampMilliseconds,
            static (item, id, _, _, title, cover) =>
                id.Length == 0
                    ? null
                    : new Post
                    {
                        Id = id,
                        Title = title,
                        PureText = title,
                        Images = !string.IsNullOrWhiteSpace(cover)
                            ? [new CircleImage { Url = cover }]
                            : [],
                        Author = new User
                        {
                            Name = item.PugcUserName,
                            AvatarUrl = item.PugcUserAvatar,
                        },
                    },
            cancellationToken
        );

    internal Task<HistoryPage<Novel>> GetNovelHistoryAsync(
        int count,
        int offset,
        bool querySoftDeleted,
        long lastMinReadTimestampMilliseconds,
        CancellationToken cancellationToken
    ) =>
        FetchHistoryAsync(
            bookType: "0",
            count,
            offset,
            querySoftDeleted,
            lastMinReadTimestampMilliseconds,
            static (item, id, _, _, title, cover) =>
                id.Length == 0
                    ? null
                    : new Novel
                    {
                        Id = id,
                        Title = title,
                        CoverUrl = cover,
                        Tags = item.GenreType == 110 ? ["漫画"] : [],
                    },
            cancellationToken
        );

    internal Task<HistoryPage<Audiobook>> GetAudiobookHistoryAsync(
        int count,
        int offset,
        bool querySoftDeleted,
        long lastMinReadTimestampMilliseconds,
        CancellationToken cancellationToken
    ) =>
        FetchHistoryAsync(
            bookType: "1",
            count,
            offset,
            querySoftDeleted,
            lastMinReadTimestampMilliseconds,
            static (item, id, _, _, title, cover) =>
                id.Length == 0
                    ? null
                    : new Audiobook
                    {
                        Id = id,
                        ListenBookId = item.ListenBookId,
                        Title = title,
                        CoverUrl = cover,
                    },
            cancellationToken
        );

    private async Task<HistoryPage<T>> FetchHistoryAsync<T>(
        string bookType,
        int count,
        int offset,
        bool querySoftDeleted,
        long lastMinReadTimestampMilliseconds,
        Func<AppHistoryItem, string, string, int, string, string, T?> targetMapper,
        CancellationToken cancellationToken
    )
        where T : class
    {
        ValidatePage(count, offset);
        AppResponse<AppHistoryData> response = await _appTransport
            .GetAsync<AppHistoryData>(
                "/reading/bookapi/read_history/list/v",
                [
                    new("book_type", bookType),
                    new("limit", "0"),
                    new("is_first_load", "false"),
                    new("offset", offset.ToString(CultureInfo.InvariantCulture)),
                    new("query_soft_deleted", querySoftDeleted ? "true" : "false"),
                    new(
                        "last_min_read_timestamp_ms",
                        lastMinReadTimestampMilliseconds.ToString(CultureInfo.InvariantCulture)
                    ),
                    new("full_field", "false"),
                ],
                cancellationToken
            )
            .ConfigureAwait(false);
        AppHistoryData data = response.Data ?? new();
        List<HistoryItem<T>> items = data.Items.Count > 0 ? new(data.Items.Count) : [];
        foreach (AppHistoryItem raw in data.Items)
        {
            if (raw is null)
            {
                continue;
            }

            AppHistoryVideoData video = raw.Video ?? new();
            string targetId = ValueNormalizer.First(
                raw.BookIdString,
                raw.SeriesId,
                raw.BookId,
                video.SeriesId
            );
            string videoId = ValueNormalizer.First(raw.VideoId, raw.VideoIdAlias, video.VideoId);
            int ep = raw.EpisodeNumber > 0 ? raw.EpisodeNumber : video.EpisodeNumber;
            string title = ValueNormalizer.First(raw.BookName, raw.Title, video.Title);
            string cover = ValueNormalizer.First(raw.ThumbUrl, raw.CoverUrl, video.CoverUrl);
            T? target = targetMapper(raw, targetId, videoId, ep, title, cover);
            if (target is null)
            {
                continue;
            }

            items.Add(
                new HistoryItem<T>
                {
                    Id = raw.Id,
                    Target = target,
                    VideoId = videoId,
                    EpisodeNumber = ep,
                    CurrentPositionMs = Math.Max(raw.CurrentPositionMs, raw.CurrentPositionAlias),
                    DurationMs = Math.Max(0, raw.DurationMs),
                    UpdatedAtUnixMilliseconds = Math.Max(
                        Math.Max(0, raw.UpdatedAtUnixMilliseconds),
                        Math.Max(0, raw.ReadTimestampMilliseconds)
                    ),
                }
            );
        }

        return new HistoryPage<T>
        {
            Items = items,
            Total = data.Total,
            NextOffset = data.NextOffset,
            HasMore = data.HasMore,
        };
    }

    internal async Task<PhoenixOperationResult> ReportProgressAsync(
        ProgressUpdate update,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(update);
        string videoId = NormalizeVideoId(update.VideoId);
        string dramaId = NormalizeDramaId(update.DramaId);
        long timestamp =
            update.UpdatedAtUnixMilliseconds > 0
                ? update.UpdatedAtUnixMilliseconds
                : DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        long bookId = long.Parse(dramaId, CultureInfo.InvariantCulture);
        long videoNumericId = long.Parse(videoId, CultureInfo.InvariantCulture);
        int videoIndex = update.EpisodeNumber > 0 ? update.EpisodeNumber : 1;
        long accumulated =
            update.AccumulatedPlayTimeMs > 0
                ? update.AccumulatedPlayTimeMs
                : Math.Max(0, update.CurrentPositionMs);
        AppReadHistoryUpdatePayload payload = new()
        {
            Items =
            [
                new AppReadHistoryUpdateItem
                {
                    BookId = bookId,
                    BookType = 2,
                    ChapterIndex = 0,
                    CurrentPlayPosition = Math.Max(0, update.CurrentPositionMs),
                    IsDelete = update.UseSoftDelete,
                    PlayerAccumulateTotalTime = accumulated,
                    ReadTimestampMs = timestamp,
                    UpdateTimestampMs = 0,
                    UseSoftDelete = update.UseSoftDelete,
                    VideoId = videoNumericId,
                    VideoIndex = videoIndex,
                },
            ],
        };
        AppResponse<AppOperationResponse> response = await _appTransport
            .PostAsync<AppReadHistoryUpdatePayload, AppOperationResponse>(
                "/reading/bookapi/read_history/update/v",
                payload,
                commentRequest: false,
                cancellationToken,
                retryMode: AppRequestRetryMode.NoRetry
            )
            .ConfigureAwait(false);
        return Succeeded(response.Message);
    }

    internal async Task<PhoenixOperationResult> DeleteHistoryAsync(
        IEnumerable<HistoryDeleteEntry> entries,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(entries);
        List<AppReadHistoryUpdateItem> items = [];
        long nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        foreach (HistoryDeleteEntry entry in entries)
        {
            if (entry is null)
            {
                continue;
            }

            if (!Enum.IsDefined(entry.ContentType))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(entries),
                    "不支持的历史记录内容类型。"
                );
            }

            string targetId = NormalizeDramaId(entry.TargetId);
            long bookId = long.Parse(targetId, CultureInfo.InvariantCulture);
            bool isVideoType = entry.ContentType
                is HistoryContentType.Drama
                    or HistoryContentType.Album
                    or HistoryContentType.ShortVideo;

            long videoId = 0;
            if (isVideoType)
            {
                string rawVid = entry.ContentType == HistoryContentType.ShortVideo
                    ? ValueNormalizer.First(entry.VideoId, targetId)
                    : entry.VideoId;
                if (!string.IsNullOrWhiteSpace(rawVid))
                {
                    videoId = long.Parse(
                        NormalizeVideoId(rawVid),
                        CultureInfo.InvariantCulture
                    );
                }
            }

            items.Add(
                new AppReadHistoryUpdateItem
                {
                    BookId = bookId,
                    BookType = (int)entry.ContentType,
                    ChapterIndex = 0,
                    CurrentPlayPosition = 0,
                    IsDelete = true,
                    ListenBookId =
                        entry.ContentType == HistoryContentType.Audiobook
                        && !string.IsNullOrWhiteSpace(entry.ListenBookId)
                            ? entry.ListenBookId.Trim()
                            : null,
                    PlayerAccumulateTotalTime = 0,
                    ReadTimestampMs = 0,
                    UpdateTimestampMs = nowMs,
                    UseSoftDelete = isVideoType,
                    VideoId = videoId,
                    VideoIndex = isVideoType
                        ? (entry.EpisodeNumber > 0 ? entry.EpisodeNumber : 1)
                        : 0,
                }
            );
        }

        if (items.Count == 0)
        {
            throw new ArgumentException("至少需要指定一条待删除的历史记录。", nameof(entries));
        }

        AppReadHistoryUpdatePayload payload = new() { Items = items };
        AppResponse<AppOperationResponse> response = await _appTransport
            .PostAsync<AppReadHistoryUpdatePayload, AppOperationResponse>(
                "/reading/bookapi/read_history/update/v",
                payload,
                commentRequest: false,
                cancellationToken,
                retryMode: AppRequestRetryMode.NoRetry
            )
            .ConfigureAwait(false);
        return Succeeded(response.Message);
    }

    internal Task<PhoenixOperationResult> DeleteHistoryAsync(
        string dramaId,
        string videoId,
        CancellationToken cancellationToken
    ) =>
        DeleteHistoryAsync(
            [HistoryDeleteEntry.FromDrama(dramaId, videoId)],
            cancellationToken
        );
}
