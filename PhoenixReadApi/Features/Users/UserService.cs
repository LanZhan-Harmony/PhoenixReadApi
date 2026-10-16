using PhoenixReadApi.Internal.Contracts.App;
using PhoenixReadApi.Internal.Contracts.App.Comment;
using PhoenixReadApi.Internal.Features.Shared;
using PhoenixReadApi.Internal.Infrastructure.Runtime;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Features.Users;

/// <summary>加载任意公开账号的主页及其公开内容。</summary>
internal sealed class UserService : PhoenixFeatureService
{
    private const int PageSize = 18;

    internal UserService(PhoenixReadClientRuntime runtime)
        : base(runtime) { }

    internal Task<UserProfile> GetAsync(string userId, CancellationToken cancellationToken)
    {
        userId = NormalizeUserId(userId);
        return LoadAsync(new User { Id = userId }, cancellationToken);
    }

    internal Task<UserProfile> GetAsync(
        DramaCredit credit,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(credit);
        ArgumentNullException.ThrowIfNull(credit.User);
        var userId = NormalizeUserId(credit.User.Id);
        return LoadAsync(
            new User
            {
                Id = userId,
                Name = credit.User.Name,
                Introduction = credit.User.Introduction,
                AvatarUrl = credit.User.AvatarUrl,
            },
            cancellationToken
        );
    }

    private async Task<UserProfile> LoadAsync(User user, CancellationToken cancellationToken)
    {
        AppUserProfilePayload metadataPayload = new()
        {
            Count = 0,
            TabType = 0,
            TargetUserId = user.Id,
        };
        AppResponse<AppUserProfileData> metadataResponse = await _appTransport
            .PostAsync<AppUserProfilePayload, AppUserProfileData>(
                "/novel/commentapi/user/profile/" + user.Id + "/v1",
                metadataPayload,
                commentRequest: true,
                cancellationToken,
                commentSource: 1506,
                serverChannel: null
            )
            .ConfigureAwait(false);
        AppUserProfileData metadata =
            metadataResponse.Data
            ?? throw new PhoenixReadApiResponseException("账号主页未返回有效数据。", "user-profile");

        HashSet<int> tabTypes = metadata.Tabs.Select(static tab => tab.Type).ToHashSet();
        Task<AppUserProfileData?> relatedTask = tabTypes.Contains(8)
            ? LoadProfileTabAsync(user.Id, 8, cancellationToken)
            : Task.FromResult<AppUserProfileData?>(null);
        Task<AppUserProfileData?> activitiesTask = tabTypes.Contains(5)
            ? LoadProfileTabAsync(user.Id, 5, cancellationToken)
            : Task.FromResult<AppUserProfileData?>(null);
        Task<AppUserShelfData?> collectionsTask = tabTypes.Contains(10)
            ? LoadCollectionsAsync(user.Id, cancellationToken)
            : Task.FromResult<AppUserShelfData?>(null);
        Task<AppUserActionData?> likesTask = tabTypes.Contains(9)
            ? LoadLikesAsync(user.Id, cancellationToken)
            : Task.FromResult<AppUserActionData?>(null);

        await Task.WhenAll(relatedTask, activitiesTask, collectionsTask, likesTask)
            .ConfigureAwait(false);

        AppUserProfileData? related = await relatedTask.ConfigureAwait(false);
        AppUserProfileData? activities = await activitiesTask.ConfigureAwait(false);
        AppUserShelfData? collections = await collectionsTask.ConfigureAwait(false);
        AppUserActionData? likes = await likesTask.ConfigureAwait(false);

        return new UserProfile
        {
            User = new User
            {
                Id = user.Id,
                Name = user.Name,
                Introduction = user.Introduction,
                AvatarUrl = user.AvatarUrl,
            },
            Collections = MapCollections(collections),
            Likes = MapLikes(likes),
            Activities = MapActivities(activities),
            RelatedDramas = MapRelatedDramas(related),
        };
    }

    private async Task<AppUserProfileData?> LoadProfileTabAsync(
        string userId,
        int tabType,
        CancellationToken cancellationToken
    )
    {
        AppUserProfilePayload payload = new()
        {
            Count = PageSize,
            TabType = tabType,
            TargetUserId = userId,
        };
        AppResponse<AppUserProfileData> response = await _appTransport
            .PostAsync<AppUserProfilePayload, AppUserProfileData>(
                "/novel/commentapi/user/profile/" + userId + "/v1",
                payload,
                commentRequest: true,
                cancellationToken,
                commentSource: 1506,
                serverChannel: null
            )
            .ConfigureAwait(false);
        return response.Data;
    }

    private async Task<AppUserShelfData?> LoadCollectionsAsync(
        string userId,
        CancellationToken cancellationToken
    )
    {
        AppResponse<AppUserShelfData> response = await _appTransport
            .GetAsync<AppUserShelfData>(
                "/reading/bookapi/bookshelf/video/info/v",
                [
                    new("count", PageSize.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                    new("offset", "0"),
                    new("target_user_id", userId),
                ],
                cancellationToken
            )
            .ConfigureAwait(false);
        return response.Data;
    }

    private async Task<AppUserActionData?> LoadLikesAsync(
        string userId,
        CancellationToken cancellationToken
    )
    {
        AppResponse<AppUserActionData> response = await _appTransport
            .GetAsync<AppUserActionData>(
                "/reading/ugc/action/mget/v",
                [
                    new("count", PageSize.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                    new("object_type_list", "6,15,10"),
                    new("target_user_id", userId),
                    new("offset", "0"),
                    new("action_type", "3"),
                ],
                cancellationToken
            )
            .ConfigureAwait(false);
        return response.Data;
    }

    private static IReadOnlyList<Drama> MapRelatedDramas(AppUserProfileData? data)
    {
        if (data is null)
        {
            return [];
        }

        List<Drama> result = [];
        HashSet<string> seen = new(StringComparer.Ordinal);
        foreach (AppUserWorkRecord? record in data.Items ?? [])
        {
            if (record?.Video is null)
            {
                continue;
            }

            var id = record.Video.Id.Trim();
            if (!JsonValueReader.IsNumericId(id) || !seen.Add(id))
            {
                continue;
            }

            AppUserVideoCommon common = record.Video.Common;
            result.Add(
                DramaMapper.FromRelatedDrama(
                    id,
                    common.Title,
                    common.Introduction,
                    common.CoverUrl,
                    record.Video.Expand.EpisodeCount
                )
            );
        }

        return result;
    }

    private static IReadOnlyList<UserActivity> MapActivities(AppUserProfileData? data)
    {
        if (data is null)
        {
            return [];
        }

        List<UserActivity> result = [];
        HashSet<string> seen = new(StringComparer.Ordinal);
        foreach (AppUserWorkRecord? record in data.Items ?? [])
        {
            if (record?.Video is null)
            {
                continue;
            }

            AppUserVideo video = record.Video;
            var id = video.Id.Trim();
            if (id.Length == 0 || !seen.Add(id))
            {
                continue;
            }

            result.Add(
                new UserActivity
                {
                    Id = id,
                    Text = video.Common.Title,
                    CoverUrl = video.Common.CoverUrl,
                    CreatedAtUnixSeconds = Math.Max(0, video.Common.CreatedAt),
                    LikeCount = Math.Max(0, video.Stat.LikeCount),
                    ViewCount = Math.Max(0, video.Stat.ViewCount),
                }
            );
        }

        return result;
    }

    private static IReadOnlyList<Drama> MapCollections(AppUserShelfData? data)
    {
        if (data is null)
        {
            return [];
        }

        List<Drama> result = [];
        HashSet<string> seen = new(StringComparer.Ordinal);
        foreach (AppUserShelfItem? item in data.Items ?? [])
        {
            if (item is null)
            {
                continue;
            }

            var id = JsonValueReader.First(item.Video.SeriesId, item.Video.VideoId).Trim();
            if (!JsonValueReader.IsNumericId(id) || !seen.Add(id))
            {
                continue;
            }

            result.Add(
                DramaMapper.FromShelf(
                    id,
                    item.SeriesTitle,
                    item.Introduction,
                    item.CoverUrl,
                    item.Video.EpisodeCount,
                    item.SeriesStatus,
                    item.Tags
                )
            );
        }

        return result;
    }

    private static IReadOnlyList<Drama> MapLikes(AppUserActionData? data)
    {
        if (data is null)
        {
            return [];
        }

        List<Drama> result = [];
        HashSet<string> seen = new(StringComparer.Ordinal);
        foreach (AppUserActionRecord? record in data.Items ?? [])
        {
            if (record?.Video is null)
            {
                continue;
            }

            AppUserActionVideoData video = record.Video;
            AppUserActionSeriesDetail series = video.Series;
            var id = JsonValueReader.First(series.SeriesId, video.SeriesId).Trim();
            if (!JsonValueReader.IsNumericId(id) || !seen.Add(id))
            {
                continue;
            }

            result.Add(
                DramaMapper.FromAction(
                    id,
                    series.Title,
                    series.Introduction.Length > 0 ? series.Introduction : video.Description,
                    series.CoverUrl.Length > 0 ? series.CoverUrl : video.CoverUrl,
                    Math.Max(series.EpisodeCount, video.EpisodeCount),
                    series.Status,
                    series.Tags
                )
            );
        }

        return result;
    }

    private static string NormalizeUserId(string value)
    {
        value = (value ?? string.Empty).Trim();
        if (!JsonValueReader.IsNumericId(value))
        {
            throw new ArgumentException("账号 ID 无效。", nameof(value));
        }

        return value;
    }
}



