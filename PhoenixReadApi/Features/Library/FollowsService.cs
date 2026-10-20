using System.Globalization;
using PhoenixReadApi.Internal.Contracts.App;
using PhoenixReadApi.Internal.Contracts.App.Comment;
using PhoenixReadApi.Internal.Contracts.App.Library;
using PhoenixReadApi.Internal.Features.Shared;
using PhoenixReadApi.Internal.Infrastructure.Http;
using PhoenixReadApi.Internal.Infrastructure.Runtime;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Features.Library;

/// <summary>账号关注关系和关注动态。</summary>
internal sealed class FollowsService : LibraryServiceBase
{
    internal FollowsService(PhoenixReadClientRuntime runtime)
        : base(runtime) { }

    internal async Task<PhoenixOperationResult> SetFollowAsync(
        string userId,
        bool active,
        CancellationToken cancellationToken
    )
    {
        if (!ValueNormalizer.IsNumericId(userId = (userId ?? string.Empty).Trim()))
        {
            throw new ArgumentException("账号 ID 无效。", nameof(userId));
        }
        AppResponse<AppOperationResponse> response = await _appTransport
            .PostAsync<AppFollowActionPayload, AppOperationResponse>(
                "/novel/articleapi/do_action/v1/",
                new AppFollowActionPayload
                {
                    ObjectId = userId,
                    ObjectType = 1,
                    ActionType = 25,
                    BusinessParameter = new AppFollowBusinessParameter
                    {
                        SharkParameter = new AppSharkParameter(),
                    },
                },
                commentRequest: false,
                cancellationToken,
                retryMode: AppRequestRetryMode.NoRetry
            )
            .ConfigureAwait(false);
        return Succeeded(response.Message);
    }

    internal async Task<FollowUpdatePage> GetFollowUpdatesAsync(
        int count,
        string cursor,
        CancellationToken cancellationToken
    )
    {
        ValidateCount(count);
        cursor = NormalizeCursor(cursor);
        AppResponse<AppFollowData> response = await _appTransport
            .GetAsync<AppFollowData>(
                "/novel/articleapi/relation/get_user_action/v1/",
                [
                    new("action_channel", "1"),
                    new("count", count.ToString(CultureInfo.InvariantCulture)),
                    new("cursor_str", cursor),
                ],
                cancellationToken
            )
            .ConfigureAwait(false);
        AppFollowData data = response.Data ?? new();
        AppFollowListInfo listInfo = data.ListInfo ?? new();
        return new FollowUpdatePage
        {
            Items = [.. data.Items.Select(MapFollow)],
            Cursor = listInfo.Cursor,
            HasMore = listInfo.HasMore,
            Total = data.Items.Count,
        };
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
}
