using System.Globalization;
using PhoenixReadApi.Internal.Contracts.App;
using PhoenixReadApi.Internal.Contracts.App.Library;
using PhoenixReadApi.Internal.Infrastructure.Http;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Features.Library;

/// <summary>阅读历史、进度、收藏、点赞、预约和分类等 App 接口。</summary>
internal sealed partial class LibraryService
{
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

    internal async Task<ReadProgressPage> GetReadProgressAsync(CancellationToken cancellationToken)
    {
        AppReadProgressResponse response = await _appTransport
            .GetReadProgressAsync("/reading/bookapi/read_progress/list/v", [], cancellationToken)
            .ConfigureAwait(false);
        return MapProgress(response);
    }

    internal async Task<ReadProgressPage> GetReadProgressForAsync(
        IReadOnlyList<string> seriesOrVideoIds,
        CancellationToken cancellationToken
    )
    {
        if (seriesOrVideoIds.Count == 0 || seriesOrVideoIds.Count > 200)
        {
            throw new ArgumentException(
                "至少提供一个、最多提供 200 个 ID。",
                nameof(seriesOrVideoIds)
            );
        }

        string[] ids =
        [
            .. seriesOrVideoIds
                .Select(static id => id.Trim())
                .Where(static id => ValueNormalizer.IsNumericId(id))
                .Distinct(StringComparer.Ordinal),
        ];
        if (ids.Length == 0)
        {
            throw new ArgumentException("播放进度查询 ID 无效。", nameof(seriesOrVideoIds));
        }

        long[] numericIds = [.. ids.Select(long.Parse)];
        try
        {
            AppReadProgressResponse response = await _appTransport
                .PostReadProgressAsync(
                    "/reading/bookapi/read_progress/get/v",
                    new AppProgressForPayload { BookIds = numericIds },
                    cancellationToken
                )
                .ConfigureAwait(false);
            return MapProgress(response);
        }
        catch (PhoenixReadApiProviderException exception) when (exception.ProviderCode == "100103")
        {
            // Some nodes reject the batch endpoint for anonymous devices. The list endpoint
            // contains the same records and is safe to filter locally.
            ReadProgressPage all = await GetReadProgressAsync(cancellationToken)
                .ConfigureAwait(false);
            HashSet<string> wanted = ids.ToHashSet(StringComparer.Ordinal);
            return new ReadProgressPage
            {
                Items =
                [
                    .. all.Items.Where(item =>
                        wanted.Contains(item.SeriesId) || wanted.Contains(item.VideoId)
                    ),
                ],
                LocalItems =
                [
                    .. all.LocalItems.Where(item =>
                        wanted.Contains(item.SeriesId) || wanted.Contains(item.VideoId)
                    ),
                ],
            };
        }
    }

    internal async Task<PhoenixOperationResult> ReportProgressAsync(
        ProgressUpdate update,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(update);
        string videoId = NormalizeVideoId(update.VideoId);
        string seriesId = NormalizeSeriesId(update.SeriesId);
        var timestamp =
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
}
