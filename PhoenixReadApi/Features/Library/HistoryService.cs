using System.Globalization;
using PhoenixReadApi.Internal.Contracts.App;
using PhoenixReadApi.Internal.Contracts.App.Library;
using PhoenixReadApi.Internal.Infrastructure.Http;
using PhoenixReadApi.Internal.Infrastructure.Runtime;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Features.Library;

/// <summary>阅读历史及播放进度上报服务。</summary>
internal sealed class HistoryService : LibraryServiceBase
{
    internal HistoryService(PhoenixReadClientRuntime runtime)
        : base(runtime) { }

    internal async Task<ReadHistoryPage> GetHistoryAsync(
        int count,
        int offset,
        bool querySoftDeleted,
        long lastMinReadTimestampMilliseconds,
        CancellationToken cancellationToken
    )
    {
        ValidatePage(count, offset);
        AppResponse<AppHistoryData> response = await _appTransport
            .GetAsync<AppHistoryData>(
                "/reading/bookapi/read_history/list/v",
                [
                    new("book_type", "2"),
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
        return new ReadHistoryPage
        {
            Items =
            [
                .. data.Items.Select(MapHistory).Where(static item => item.VideoId.Length > 0),
            ],
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
        string seriesId = NormalizeSeriesId(update.SeriesId);
        long timestamp =
            update.UpdatedAtUnixMilliseconds > 0
                ? update.UpdatedAtUnixMilliseconds
                : DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        long bookId = long.Parse(seriesId, CultureInfo.InvariantCulture);
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

    internal Task<PhoenixOperationResult> DeleteHistoryAsync(
        string seriesId,
        string videoId,
        CancellationToken cancellationToken
    ) =>
        ReportProgressAsync(
            new ProgressUpdate
            {
                SeriesId = seriesId,
                VideoId = videoId,
                UseSoftDelete = true,
            },
            cancellationToken
        );

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

}
