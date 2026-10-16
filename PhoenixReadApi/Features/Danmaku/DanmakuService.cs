using System.Text.Json;
using PhoenixReadApi.Internal.Contracts.App;
using PhoenixReadApi.Internal.Contracts.App.Comment;
using PhoenixReadApi.Internal.Contracts.App.Danmaku;
using PhoenixReadApi.Internal.Features.Shared;
using PhoenixReadApi.Internal.Infrastructure.Runtime;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Features.Danmaku;

/// <summary>加载并清理指定时间范围内的 App 弹幕。</summary>
internal sealed class DanmakuService : PhoenixFeatureService
{
    private const int MaxDanmakuDurationMs = 24 * 60 * 60 * 1000;
    private const int MaxDanmakuItems = 90;

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
        var series = NormalizeSeriesId(seriesId);
        var video = NormalizeVideoId(videoId);
        if (startMs < 0 || durationMs <= startMs || durationMs > MaxDanmakuDurationMs)
        {
            throw new ArgumentOutOfRangeException(
                nameof(durationMs),
                "Danmaku duration must be after start and at most 24 hours."
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
        AppCommentPayload payload = new()
        {
            GroupId = videoId,
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
        AppDanmakuData data =
            result.Data
            ?? throw new PhoenixReadApiResponseException("红果弹幕时间段格式无效。", "danmaku");

        var next = data.Extra?.NextTime ?? 0;
        if (next <= startMs || next > MaxDanmakuDurationMs)
        {
            throw new PhoenixReadApiResponseException("红果弹幕时间段格式无效。", "danmaku");
        }

        DanmakuPage page = new()
        {
            EpisodeId = videoId,
            StartMs = startMs,
            NextMs = Math.Min(next, durationMs),
            Total = ParseTotal(data),
        };
        List<DanmakuItem> items = [];
        HashSet<string> seen = new(StringComparer.Ordinal);
        foreach (AppCommentRecord? raw in data.Items ?? [])
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

            var position = comment.Expand?.OffsetTime ?? 0;
            var text = SanitizeText(common.Content?.Text ?? string.Empty);
            var id = (comment.Id ?? string.Empty).Trim();
            if (
                position < startMs
                || position >= page.NextMs
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
            if (items.Count == MaxDanmakuItems)
            {
                break;
            }
        }

        items.Sort(static (left, right) => left.TimeMs.CompareTo(right.TimeMs));
        return new DanmakuPage
        {
            EpisodeId = page.EpisodeId,
            StartMs = page.StartMs,
            NextMs = page.NextMs,
            Total = page.Total,
            Items = items,
        };
    }

    private static long ParseTotal(AppDanmakuData data)
    {
        var cursor = data.ListInfo?.Cursor ?? string.Empty;
        if (cursor.Length is 0 or > 8192)
        {
            return 0;
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(cursor);
            return Math.Max(0, JsonValueReader.Long(document.RootElement, "danmaku_count"));
        }
        catch (JsonException)
        {
            return 0;
        }
    }

    private static string SanitizeText(string text) =>
        new string([
            .. text.Select(static c => char.IsControl(c) || c is '\u2028' or '\u2029' ? ' ' : c),
        ]).Trim();
}
