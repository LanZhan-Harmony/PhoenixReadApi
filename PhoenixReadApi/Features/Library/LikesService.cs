using System.Globalization;
using PhoenixReadApi.Internal.Contracts.App;
using PhoenixReadApi.Internal.Contracts.App.Comment;
using PhoenixReadApi.Internal.Contracts.App.Library;
using PhoenixReadApi.Internal.Features.Shared;
using PhoenixReadApi.Internal.Infrastructure.Http;
using PhoenixReadApi.Internal.Infrastructure.Runtime;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Features.Library;

/// <summary>剧集点赞列表以及分集点赞操作。</summary>
internal sealed class LikesService : LibraryServiceBase
{
    internal LikesService(PhoenixReadClientRuntime runtime)
        : base(runtime) { }

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

    internal async Task<DramaPage> GetLikesAsync(
        int count,
        int offset,
        CancellationToken cancellationToken
    )
    {
        ValidatePage(count, offset);
        AppResponse<AppUserActionData> response = await _appTransport
            .GetAsync<AppUserActionData>(
                "/reading/ugc/action/mget/v",
                [
                    new("count", count.ToString(CultureInfo.InvariantCulture)),
                    new("object_type_list", "6,15,10"),
                    new("offset", offset.ToString(CultureInfo.InvariantCulture)),
                    new("action_type", "3"),
                ],
                cancellationToken
            )
            .ConfigureAwait(false);
        AppUserActionData data = response.Data ?? new();
        return new DramaPage
        {
            Items =
            [
                .. (data.Items ?? [])
                    .Where(static item => item is not null)
                    .Select(MapLike)
                    .Where(static item => item.Id.Length > 0),
            ],
            Total = data.Total,
            NextOffset = data.NextOffset,
            HasMore = data.HasMore,
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
}
