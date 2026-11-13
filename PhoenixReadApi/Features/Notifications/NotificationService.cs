using System.Collections.Concurrent;
using PhoenixReadApi.Internal.Contracts.App;
using PhoenixReadApi.Internal.Contracts.App.Notifications;
using PhoenixReadApi.Internal.Features.Shared;
using PhoenixReadApi.Internal.Infrastructure.Http;
using PhoenixReadApi.Internal.Infrastructure.Runtime;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Features.Notifications;

/// <summary>短剧追更提醒与通知中心功能服务。</summary>
internal sealed class NotificationService : PhoenixFeatureService
{
    private const int DefaultCount = 20;
    private const int MaxCount = 50;

    private readonly ConcurrentDictionary<string, NoticeItem> _notices = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, int> _lastKnownEpisodes = new(StringComparer.Ordinal);

    internal NotificationService(PhoenixReadClientRuntime runtime)
        : base(runtime) { }

    internal async Task<bool> SetFollowUpdateAsync(
        string dramaId,
        bool follow,
        CancellationToken cancellationToken
    )
    {
        dramaId = NormalizeDramaId(dramaId);
        AppFollowUpdateActionPayload payload = new()
        {
            ActionCategory = 104,
            ActionType = follow ? 1 : 2,
            ItemId = dramaId,
            ObjectId = dramaId,
            ObjectType = 1,
            ModifyCount = 1,
            EnterFrom = "detail",
            BusinessParam = "{\"enter_from\":\"detail\"}",
        };

        AppResponse<AppOperationResponse> response = await _appTransport
            .PostAsync<AppFollowUpdateActionPayload, AppOperationResponse>(
                "/reading/articleapi/relation/update/v1/",
                payload,
                commentRequest: false,
                cancellationToken: cancellationToken,
                commentSource: 1506,
                serverChannel: null,
                retryMode: AppRequestRetryMode.NoRetry
            )
            .ConfigureAwait(false);

        return response.Code is "0" or "";
    }

    internal async Task<FollowUpdatePage> GetFollowUpdatesAsync(
        string cursor,
        int count,
        CancellationToken cancellationToken
    )
    {
        ValidateCount(count);
        string normalizedCursor = NormalizeCursor(cursor);

        AppResponse<AppFollowUpdateListData> response = await _appTransport
            .GetAsync<AppFollowUpdateListData>(
                "/novel/articleapi/relation/get_user_action/v1/",
                [
                    new("action_channel", "9"),
                    new("count", count.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                    new("cursor_str", normalizedCursor),
                ],
                cancellationToken
            )
            .ConfigureAwait(false);

        AppFollowUpdateListData? data = response.Data;
        List<FollowUpdateItem> items = [];
        HashSet<string> seen = new(StringComparer.Ordinal);

        foreach (AppFollowUpdateRecord? record in data?.Items ?? [])
        {
            if (record?.Video is null)
            {
                continue;
            }

            string id = record.Video.Id.Trim();
            if (!ValueNormalizer.IsNumericId(id) || !seen.Add(id))
            {
                continue;
            }

            AppFollowUpdateCommon common = record.Video.Common;
            AppFollowUpdateExpand expand = record.Video.Expand;

            items.Add(
                new FollowUpdateItem
                {
                    DramaId = id,
                    Title = ValueNormalizer.First(common.Title, id),
                    CoverUrl = common.CoverUrl,
                    TotalEpisodeCount = Math.Max(0, expand.EpisodeCount),
                    LatestWatchedEpisodeIndex = Math.Max(0, expand.LatestWatchedIndex),
                    HasWatchedLatest = expand.HasWatchedLatest,
                    IsFinished = expand.SeriesStatus == 1,
                    UpdateFrequency = expand.UpdateInfo.Frequency,
                    FollowedAtUnixMilliseconds = Math.Max(0, expand.Extra.FollowAt),
                    LastReleasedAtUnixMilliseconds = Math.Max(0, expand.Extra.LastPassAt),
                }
            );
        }

        return new FollowUpdatePage
        {
            Items = items,
            Cursor = data?.ListInfo?.Cursor ?? string.Empty,
            HasMore = data?.ListInfo?.HasMore ?? false,
        };
    }

    internal async Task<bool> IsFollowingUpdateAsync(
        string dramaId,
        CancellationToken cancellationToken
    )
    {
        dramaId = NormalizeDramaId(dramaId);
        FollowUpdatePage page = await GetFollowUpdatesAsync(string.Empty, MaxCount, cancellationToken)
            .ConfigureAwait(false);

        return page.Items.Any(item => string.Equals(item.DramaId, dramaId, StringComparison.Ordinal));
    }

    internal async Task<IReadOnlyList<NoticeItem>> CheckNoticesAsync(CancellationToken cancellationToken)
    {
        FollowUpdatePage updates = await GetFollowUpdatesAsync(string.Empty, MaxCount, cancellationToken)
            .ConfigureAwait(false);

        List<NoticeItem> newNotices = [];
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        foreach (FollowUpdateItem item in updates.Items)
        {
            if (item.TotalEpisodeCount <= 0)
            {
                continue;
            }

            int lastSeen = _lastKnownEpisodes.TryGetValue(item.DramaId, out int ep)
                ? ep
                : Math.Max(0, item.LatestWatchedEpisodeIndex);

            if (item.TotalEpisodeCount > lastSeen)
            {
                string noticeId = "notice:follow:" + item.DramaId + ":" + item.TotalEpisodeCount;
                NoticeItem notice = new()
                {
                    Id = noticeId,
                    Type = NoticeType.FollowUpdate,
                    DramaId = item.DramaId,
                    Title = item.Title,
                    CoverUrl = item.CoverUrl,
                    FromEpisode = lastSeen,
                    EpisodeCount = item.TotalEpisodeCount,
                    CreatedAtUnixMilliseconds = item.LastReleasedAtUnixMilliseconds > 0
                        ? item.LastReleasedAtUnixMilliseconds
                        : now,
                    IsRead = false,
                };

                if (_notices.TryAdd(noticeId, notice))
                {
                    newNotices.Add(notice);
                }
            }

            _lastKnownEpisodes[item.DramaId] = item.TotalEpisodeCount;
        }

        return newNotices;
    }

    internal Task<NoticePage> GetNoticesAsync(CancellationToken cancellationToken)
    {
        List<NoticeItem> list = _notices.Values
            .OrderByDescending(static n => n.CreatedAtUnixMilliseconds)
            .ToList();

        int unread = list.Count(static n => !n.IsRead);
        return Task.FromResult(new NoticePage
        {
            Items = list,
            UnreadCount = unread,
        });
    }

    internal Task MarkNoticesReadAsync(
        IEnumerable<string> noticeIds,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(noticeIds);
        HashSet<string> targetSet = new(noticeIds, StringComparer.Ordinal);

        foreach (string id in targetSet)
        {
            if (_notices.TryGetValue(id, out NoticeItem? notice))
            {
                _notices[id] = new NoticeItem
                {
                    Id = notice.Id,
                    Type = notice.Type,
                    DramaId = notice.DramaId,
                    Title = notice.Title,
                    CoverUrl = notice.CoverUrl,
                    FromEpisode = notice.FromEpisode,
                    EpisodeCount = notice.EpisodeCount,
                    CreatedAtUnixMilliseconds = notice.CreatedAtUnixMilliseconds,
                    IsRead = true,
                };
            }
        }

        return Task.CompletedTask;
    }

    internal Task ClearNoticesAsync(CancellationToken cancellationToken)
    {
        _notices.Clear();
        return Task.CompletedTask;
    }

    private static string NormalizeDramaId(string dramaId)
    {
        if (string.IsNullOrWhiteSpace(dramaId))
        {
            throw new ArgumentException("剧集 ID 不能为空。", nameof(dramaId));
        }

        string trimmed = dramaId.Trim();
        if (!ValueNormalizer.IsNumericId(trimmed))
        {
            throw new ArgumentException("剧集 ID 必须是由数字构成的字符串。", nameof(dramaId));
        }

        return trimmed;
    }

    private static void ValidateCount(int count)
    {
        if (count is < 1 or > MaxCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(count),
                count,
                "分页数量必须在 1 到 " + MaxCount + " 之间。"
            );
        }
    }

    private static string NormalizeCursor(string cursor)
    {
        if (string.IsNullOrWhiteSpace(cursor))
        {
            return string.Empty;
        }

        string trimmed = cursor.Trim();
        if (trimmed.Length > 2048)
        {
            throw new ArgumentException("分页游标长度超过允许上限。", nameof(cursor));
        }

        return trimmed;
    }
}
