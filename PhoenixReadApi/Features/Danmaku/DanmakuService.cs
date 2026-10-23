using PhoenixReadApi.Internal.Contracts.App;
using PhoenixReadApi.Internal.Contracts.App.Comment;
using PhoenixReadApi.Internal.Contracts.App.Danmaku;
using PhoenixReadApi.Internal.Features.Shared;
using PhoenixReadApi.Internal.Infrastructure.Runtime;

namespace PhoenixReadApi.Internal.Features.Danmaku;

/// <summary>加载并清理指定时间范围内的 App 弹幕。</summary>
internal sealed class DanmakuService : PhoenixFeatureService
{
    private const int MaxDanmakuDurationMs = 24 * 60 * 60 * 1000;
    private const int MaxDanmakuItemsPerRequest = 90;
    private const int MaxEmptyDanmakuRetries = 2;
    private const int MaxDanmakuRequests = 4096;

    internal DanmakuService(PhoenixReadClientRuntime runtime)
        : base(runtime) { }

    internal Task<DanmakuPage> GetAsync(
        string seriesId,
        string videoId,
        long startMs,
        long durationMs,
        CancellationToken cancellationToken
    )
    {
        string series = NormalizeSeriesId(seriesId);
        string video = NormalizeVideoId(videoId);
        if (
            startMs < 0
            || durationMs <= 0
            || durationMs > MaxDanmakuDurationMs
            || startMs > MaxDanmakuDurationMs - durationMs
        )
        {
            throw new ArgumentOutOfRangeException(
                nameof(durationMs),
                "Danmaku duration must be positive, at most 24 hours, and fit after start."
            );
        }

        return LoadAsync(series, video, startMs, durationMs, cancellationToken);
    }

    private async Task<DanmakuPage> LoadAsync(
        string seriesId,
        string videoId,
        long startMs,
        long durationMs,
        CancellationToken cancellationToken
    )
    {
        long requestedEndMs = startMs + durationMs;
        long currentMs = startMs;
        long nextMs = startMs;
        long total = 0L;
        List<DanmakuItem> items = [];
        HashSet<string> seen = new(StringComparer.Ordinal);

        for (int request = 0; request < MaxDanmakuRequests && currentMs < requestedEndMs; request++)
        {
            AppDanmakuData data = await RequestPageWithRetryAsync(
                    seriesId,
                    videoId,
                    currentMs,
                    durationMs,
                    cancellationToken
                )
                .ConfigureAwait(false);

            AppDanmakuCursor cursor = data.ListInfo?.Cursor ?? new();
            total = Math.Max(total, cursor.DanmakuCount);
            total = Math.Max(total, data.ListInfo?.Total ?? 0);

            long candidateNextMs = ResolveNextMs(currentMs, requestedEndMs, data, cursor);
            long pageEndMs = Math.Min(candidateNextMs, requestedEndMs);
            if (pageEndMs <= currentMs)
            {
                // A missing or stale cursor means the server has finished this
                // range. Returning the accumulated items is more useful than
                // turning a valid empty/last page into a format exception.
                nextMs = Math.Max(nextMs, Math.Min(currentMs, requestedEndMs));
                break;
            }

            AddItems(data.Items, videoId, currentMs, pageEndMs, items, seen);
            currentMs = pageEndMs;
            nextMs = currentMs;

            if (candidateNextMs >= requestedEndMs)
            {
                break;
            }
        }

        items.Sort(static (left, right) =>
        {
            int time = left.TimeMs.CompareTo(right.TimeMs);
            return time != 0 ? time : string.CompareOrdinal(left.Id, right.Id);
        });

        return new DanmakuPage
        {
            EpisodeId = videoId,
            StartMs = startMs,
            NextMs = nextMs,
            Total = Math.Max(total, items.Count),
            Items = items,
        };
    }

    private async Task<AppDanmakuData> RequestPageWithRetryAsync(
        string seriesId,
        string videoId,
        long startMs,
        long durationMs,
        CancellationToken cancellationToken
    )
    {
        AppDanmakuData? last = null;
        for (int attempt = 0; attempt <= MaxEmptyDanmakuRetries; attempt++)
        {
            last = await RequestPageAsync(
                    seriesId,
                    videoId,
                    startMs,
                    durationMs,
                    cancellationToken
                )
                .ConfigureAwait(false);

            if (last.Items?.Count > 0 || !HasReportedDanmaku(last))
            {
                return last;
            }

            if (attempt < MaxEmptyDanmakuRetries)
            {
                await Task.Delay(100 * (attempt + 1), cancellationToken).ConfigureAwait(false);
            }
        }

        return last!;
    }

    private async Task<AppDanmakuData> RequestPageAsync(
        string seriesId,
        string videoId,
        long startMs,
        long durationMs,
        CancellationToken cancellationToken
    )
    {
        AppCommentPayload payload = new()
        {
            GroupId = videoId,
            Count = MaxDanmakuItemsPerRequest,
            BusinessParameter = new AppCommentBusinessParameter
            {
                BookId = seriesId,
                StartOffsetTime = startMs,
                PlayletItemDuration = durationMs,
            },
        };
        AppResponse<AppDanmakuData> result = await _appTransport
            .PostAsync<AppCommentPayload, AppDanmakuData>(
                "/novel/commentapi/comment/list/" + videoId + "/v1/",
                payload,
                commentRequest: true,
                cancellationToken
            )
            .ConfigureAwait(false);
        return result.Data
            ?? throw new PhoenixReadApiResponseException("红果弹幕接口未返回有效数据。", "danmaku");
    }

    private static void AddItems(
        IReadOnlyList<AppCommentRecord>? records,
        string videoId,
        long startMs,
        long endMs,
        List<DanmakuItem> items,
        HashSet<string> seen
    )
    {
        foreach (AppCommentRecord? raw in records ?? [])
        {
            if (
                raw is null
                || raw.Comment is null
                || raw.Comment.Common is null
                || raw.Comment.Common.Content is null
            )
            {
                continue;
            }

            AppComment comment = raw.Comment;
            AppCommentCommon common = comment.Common;
            if ((common.GroupId ?? string.Empty) != videoId || common.Status != "1")
            {
                continue;
            }

            long position = comment.Expand?.OffsetTime ?? 0;
            string text = SanitizeText(common.Content?.Text ?? string.Empty);
            string id = (comment.Id ?? string.Empty).Trim();
            if (
                position < startMs
                || position >= endMs
                || text.Length == 0
                || id.Length == 0
                || id.Length > 120
                || !seen.Add(id)
            )
            {
                continue;
            }

            if (text.Length > 180)
            {
                text = string.Concat(text.AsSpan(0, 180), "…");
            }

            items.Add(
                new DanmakuItem
                {
                    Id = id,
                    Text = text,
                    TimeMs = position,
                }
            );
        }
    }

    private static long ResolveNextMs(
        long currentMs,
        long requestedEndMs,
        AppDanmakuData data,
        AppDanmakuCursor cursor
    )
    {
        long extraNextMs = data.Extra?.NextTime ?? 0;
        if (extraNextMs > currentMs)
        {
            return Math.Min(extraNextMs, requestedEndMs);
        }

        if (cursor.EndOffsetTime > currentMs)
        {
            return Math.Min(cursor.EndOffsetTime, requestedEndMs);
        }

        // Some last pages omit both the extra field and a usable end cursor.
        // Treat the requested range as complete instead of reporting a format
        // error for an otherwise valid response.
        return requestedEndMs;
    }

    private static bool HasReportedDanmaku(AppDanmakuData data)
    {
        AppDanmakuCursor cursor = data.ListInfo?.Cursor ?? new();
        return cursor.DanmakuCount > 0 || (data.ListInfo?.Total ?? 0) > 0;
    }

    private static string SanitizeText(string text) =>
        new string([
            .. text.Select(static c => char.IsControl(c) || c is '\u2028' or '\u2029' ? ' ' : c),
        ]).Trim();

}
