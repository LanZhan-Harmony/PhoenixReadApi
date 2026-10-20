using PhoenixReadApi.Internal.Contracts.App;
using PhoenixReadApi.Internal.Contracts.App.Comment;
using PhoenixReadApi.Internal.Contracts.App.Library;
using PhoenixReadApi.Internal.Infrastructure.Http;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Features.Library;

/// <summary>阅读历史、进度、收藏、点赞、预约和分类等 App 接口。</summary>
internal sealed partial class LibraryService
{
    internal async Task<PhoenixOperationResult> SetFavoriteAsync(
        string seriesId,
        string videoId,
        bool active,
        CancellationToken cancellationToken
    )
    {
        seriesId = NormalizeSeriesId(seriesId);
        videoId = NormalizeVideoId(videoId);
        AppShelfUpdatePayload payload = new()
        {
            IsActiveBehavior = true,
            OperationType = active ? 1 : 2,
            Items = [new AppShelfUpdateItem { SeriesId = seriesId, VideoId = videoId }],
        };
        AppResponse<AppOperationResponse> response = await _appTransport
            .PostAsync<AppShelfUpdatePayload, AppOperationResponse>(
                "/reading/bookapi/bookshelf/video/update/v",
                payload,
                commentRequest: false,
                cancellationToken,
                retryMode: AppRequestRetryMode.NoRetry
            )
            .ConfigureAwait(false);
        return Succeeded(response.Message);
    }

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

    internal async Task<PhoenixOperationResult> SetDramaEpisodeLikeAsync(
        string seriesId,
        string videoId,
        bool active,
        CancellationToken cancellationToken
    )
    {
        seriesId = NormalizeSeriesId(seriesId);
        videoId = NormalizeVideoId(videoId);
        AppResponse<AppOperationResponse> response = await _appTransport
            .PostAsync<AppArticleActionPayload, AppOperationResponse>(
                "/novel/articleapi/do_action/v1/",
                new AppArticleActionPayload
                {
                    ObjectId = videoId,
                    ObjectType = 6,
                    ActionType = active ? 3 : 4,
                    ActionCategory = 1,
                    BusinessParameter = new AppArticleActionBusinessParameter
                    {
                        BookId = 0,
                        VideoId = seriesId,
                        HasAigcContent = false,
                        ModifyCount = 0,
                        ScreenFrame = active
                            ? $"pic_dislike_{videoId}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{Random.Shared.Next(0x1000000):x6}"
                            : string.Empty,
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

    private async Task<PhoenixOperationResult> SetReservationCoreAsync(
        string seriesId,
        bool active,
        CancellationToken cancellationToken
    )
    {
        seriesId = NormalizeSeriesId(seriesId);
        AppResponse<AppOperationResponse> response = await _appTransport
            .PostAsync<AppReservationActionPayload, AppOperationResponse>(
                active
                    ? "/reading/bookapi/search/uncover_subscribe/v"
                    : "/reading/user/subscribe/delete/v",
                new AppReservationActionPayload
                {
                    ItemId = seriesId,
                    ItemType = 14,
                    OperationType = active ? 1 : 2,
                    WishListAllDelete = 0,
                    NotDeleteItemId = active ? string.Empty : seriesId,
                    SharkParameter = new AppSharkParameter
                    {
                        EnterFrom = "ShortSeriesActivity",
                        PreviousPage = "MainFragmentActivity",
                        PageList = "MainFragmentActivity,ShortSeriesActivity",
                    },
                },
                commentRequest: false,
                cancellationToken,
                retryMode: AppRequestRetryMode.NoRetry
            )
            .ConfigureAwait(false);
        return Succeeded(response.Message);
    }
}
