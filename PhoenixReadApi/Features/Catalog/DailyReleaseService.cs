using System.Globalization;
using PhoenixReadApi.Internal.Contracts.App.Catalog;
using PhoenixReadApi.Internal.Features.Shared;
using PhoenixReadApi.Internal.Infrastructure.Runtime;

namespace PhoenixReadApi.Internal.Features.Catalog;

/// <summary>负责按指定日期查询短剧上新日历服务。</summary>
internal sealed class DailyReleaseService : PhoenixFeatureService
{
    internal DailyReleaseService(PhoenixReadClientRuntime runtime)
        : base(runtime) { }

    internal async Task<DailyReleasePage> GetDailyReleasesAsync(
        DateOnly? targetDate,
        int offset,
        string? sessionId,
        CancellationToken cancellationToken
    )
    {
        string dateStr = targetDate.HasValue
            ? targetDate.Value.ToString("yyyyMMdd", CultureInfo.InvariantCulture)
            : string.Empty;

        List<KeyValuePair<string, string>> query =
        [
            new("tab_type", "5"),
            new("offset", Math.Max(0, offset).ToString(CultureInfo.InvariantCulture)),
            new("active_panel", "5"),
            new("gender_type", "2"),
            new("filter_type", "gender"),
            new("tab_style", "2"),
            new("need_calendar_schema", "true"),
            new("order_experiment", "descend"),
            new("subscribe_list_opt", "0"),
            new("version", "0"),
            new("user_is_login", "0"),
        ];

        if (!string.IsNullOrWhiteSpace(dateStr))
        {
            query.Add(new("target_date", dateStr));
        }

        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            query.Add(new("session_id", sessionId));
        }

        AppDailyReleaseResponse response = await _appTransport
            .GetRootAsync<AppDailyReleaseResponse>(
                "/reading/user/subscribe/list/v1/",
                query,
                cancellationToken
            )
            .ConfigureAwait(false);

        AppDailyReleaseData? data = response.Data;
        List<DailyReleaseItem> items = [];
        HashSet<string> seen = new(StringComparer.Ordinal);

        foreach (AppDailyReleaseItemDto item in data?.SubscribeItems ?? [])
        {
            string sid;
            string title;
            string desc;
            string cover;
            int epCount = 0;

            if (
                item.SubscribeData is not null
                && !string.IsNullOrWhiteSpace(item.SubscribeData.SeriesId)
            )
            {
                sid = item.SubscribeData.SeriesId;
                title = item.SubscribeData.Title;
                desc = item.SubscribeData.VideoDesc;
                cover = item.SubscribeData.Cover;
                epCount = item.SubscribeData.EpisodeCount;
            }
            else
            {
                sid = item.ItemId;
                title = item.Name;
                desc = item.ItemDesc;
                cover = item.Cover;
            }

            if (string.IsNullOrWhiteSpace(sid) || sid == "0" || string.IsNullOrWhiteSpace(title))
            {
                continue;
            }

            if (!seen.Add(sid))
            {
                continue;
            }

            long publishSeconds =
                item.SchedulePublishTime > 0 ? item.SchedulePublishTime : item.ExpectedPublishTime;

            DateTimeOffset? publishTime =
                publishSeconds > 0 ? DateTimeOffset.FromUnixTimeSeconds(publishSeconds) : null;

            Drama drama = new()
            {
                Id = sid,
                Title = title,
                Description = desc,
                CoverUrl = cover,
                EpisodeCount = epCount,
                Remark = item.SubInfo,
                Tags = item.Categories,
                ReleaseStatus = item.IsOnline
                    ? DramaReleaseStatus.Finished
                    : DramaReleaseStatus.Ongoing,
            };

            items.Add(
                new DailyReleaseItem
                {
                    Drama = drama,
                    IsOnline = item.IsOnline,
                    PublishTime = publishTime,
                    Note = item.SubInfo,
                    Categories = item.Categories,
                }
            );
        }

        DateOnly currentDate = targetDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        if (
            !targetDate.HasValue
            && DateOnly.TryParseExact(
                data?.CalendarSchema.DefaultDate,
                "yyyyMMdd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateOnly parsedDefault
            )
        )
        {
            currentDate = parsedDefault;
        }

        List<DateOnly> availableDates = [];
        foreach (string dateItem in data?.CalendarSchema.DateList ?? [])
        {
            if (
                DateOnly.TryParseExact(
                    dateItem,
                    "yyyyMMdd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateOnly parsed
                )
            )
            {
                availableDates.Add(parsed);
            }
        }

        int nextOffset = data?.NextOffset ?? 0;
        if (nextOffset == 0 && items.Count > 0)
        {
            nextOffset = offset + items.Count;
        }

        return new DailyReleasePage
        {
            Date = currentDate,
            AvailableDates = availableDates,
            Items = items,
            NextOffset = nextOffset,
            HasMore = data?.HasMore ?? false,
            SessionId = data?.SessionId ?? string.Empty,
        };
    }
}
