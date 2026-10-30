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
    private const int MaxPageSize = 50;

    internal UserService(PhoenixReadClientRuntime runtime)
        : base(runtime) { }

    internal Task<UserProfile> GetAsync(string userId, CancellationToken cancellationToken)
    {
        userId = NormalizeUserId(userId);
        return LoadAsync(new User { Id = userId }, cancellationToken);
    }

    internal Task<UserProfile> GetAsync(DramaCredit credit, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(credit);
        ArgumentNullException.ThrowIfNull(credit.User);
        string userId = NormalizeUserId(credit.User.Id);
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

    internal async Task<UserDramaPage> GetRelatedDramasAsync(
        string userId,
        int count,
        string cursor,
        CancellationToken cancellationToken
    )
    {
        userId = NormalizeUserId(userId);
        ValidateCount(count);
        cursor = NormalizeCursor(cursor);
        AppUserProfileData? data = await LoadProfileTabAsync(
                userId,
                8,
                count,
                cursor,
                cancellationToken
            )
            .ConfigureAwait(false);
        return new UserDramaPage
        {
            Items = MapRelatedDramas(data),
            Cursor = data?.ListInfo?.Cursor ?? string.Empty,
            HasMore = data?.ListInfo?.HasMore ?? false,
            Total = TabTotal(data, 8),
        };
    }

    internal async Task<UserActivityPage> GetActivitiesAsync(
        string userId,
        int count,
        string cursor,
        CancellationToken cancellationToken
    )
    {
        userId = NormalizeUserId(userId);
        ValidateCount(count);
        cursor = NormalizeCursor(cursor);
        AppUserProfileData? data = await LoadProfileTabAsync(
                userId,
                5,
                count,
                cursor,
                cancellationToken
            )
            .ConfigureAwait(false);
        return new UserActivityPage
        {
            Items = MapActivities(data),
            Cursor = data?.ListInfo?.Cursor ?? string.Empty,
            HasMore = data?.ListInfo?.HasMore ?? false,
            Total = TabTotal(data, 5),
        };
    }

    internal async Task<UserDramaPage> GetCollectionsAsync(
        string userId,
        int count,
        int offset,
        CancellationToken cancellationToken
    )
    {
        userId = NormalizeUserId(userId);
        ValidateCount(count);
        ValidateOffset(offset);
        AppUserShelfData? data = await LoadCollectionsAsync(
                userId,
                count,
                offset,
                cancellationToken
            )
            .ConfigureAwait(false);
        return new UserDramaPage
        {
            Items = MapCollections(data),
            NextOffset = data?.NextOffset ?? offset,
            HasMore = data?.HasMore ?? false,
            Total = data?.Total ?? 0,
        };
    }

    internal async Task<UserDramaPage> GetLikesAsync(
        string userId,
        int count,
        int offset,
        CancellationToken cancellationToken
    )
    {
        userId = NormalizeUserId(userId);
        ValidateCount(count);
        ValidateOffset(offset);
        AppUserActionData? data = await LoadLikesAsync(userId, count, offset, cancellationToken)
            .ConfigureAwait(false);
        return new UserDramaPage
        {
            Items = MapLikes(data),
            NextOffset = data?.NextOffset ?? offset,
            HasMore = data?.HasMore ?? false,
            Total = data?.Total ?? 0,
        };
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
            ?? throw new PhoenixReadApiResponseException(
                "账号主页未返回有效数据。",
                "user-profile"
            );

        HashSet<int> tabTypes =
        [
            .. (metadata.Tabs ?? [])
                .Where(static tab => tab is not null)
                .Select(static tab => tab.Type),
        ];
        Task<AppUserProfileData?> relatedTask = tabTypes.Contains(8)
            ? LoadProfileTabAsync(user.Id, 8, PageSize, string.Empty, cancellationToken)
            : Task.FromResult<AppUserProfileData?>(null);
        Task<AppUserProfileData?> activitiesTask = tabTypes.Contains(5)
            ? LoadProfileTabAsync(user.Id, 5, PageSize, string.Empty, cancellationToken)
            : Task.FromResult<AppUserProfileData?>(null);
        Task<AppUserShelfData?> collectionsTask = tabTypes.Contains(10)
            ? LoadCollectionsAsync(user.Id, PageSize, 0, cancellationToken)
            : Task.FromResult<AppUserShelfData?>(null);
        Task<AppUserActionData?> likesTask = tabTypes.Contains(9)
            ? LoadLikesAsync(user.Id, PageSize, 0, cancellationToken)
            : Task.FromResult<AppUserActionData?>(null);

        await Task.WhenAll(relatedTask, activitiesTask, collectionsTask, likesTask)
            .ConfigureAwait(false);

        AppUserProfileData? related = await relatedTask.ConfigureAwait(false);
        AppUserProfileData? activities = await activitiesTask.ConfigureAwait(false);
        AppUserShelfData? collections = await collectionsTask.ConfigureAwait(false);
        AppUserActionData? likes = await likesTask.ConfigureAwait(false);

        AppCommentUserInfo? userInfo = metadata.UserInfo;
        string resolvedName = ValueNormalizer.First(
            user.Name,
            userInfo?.Name,
            userInfo?.BaseInfo?.Name
        );
        string resolvedAvatar = ValueNormalizer.First(
            user.AvatarUrl,
            userInfo?.AvatarUrl,
            userInfo?.BaseInfo?.AvatarUrl
        );
        string resolvedIntro = ValueNormalizer.First(
            user.Introduction,
            userInfo?.Description,
            userInfo?.Introduction,
            userInfo?.BaseInfo?.Description,
            userInfo?.BaseInfo?.Introduction
        );

        return new UserProfile
        {
            User = new User
            {
                Id = user.Id,
                Name = resolvedName,
                Introduction = resolvedIntro,
                AvatarUrl = resolvedAvatar,
            },
            Collections = MapCollections(collections),
            CollectionsPage = new UserPageInfo
            {
                NextOffset = collections?.NextOffset ?? 0,
                HasMore = collections?.HasMore ?? false,
                Total = Math.Max(collections?.Total ?? 0, TabTotal(metadata, 10)),
            },
            Likes = MapLikes(likes),
            LikesPage = new UserPageInfo
            {
                NextOffset = likes?.NextOffset ?? 0,
                HasMore = likes?.HasMore ?? false,
                Total = Math.Max(likes?.Total ?? 0, TabTotal(metadata, 9)),
            },
            Activities = MapActivities(activities),
            ActivitiesPage = ProfilePageInfo(activities, metadata, 5),
            RelatedDramas = MapRelatedDramas(related),
            RelatedDramasPage = ProfilePageInfo(related, metadata, 8),
        };
    }

    private async Task<AppUserProfileData?> LoadProfileTabAsync(
        string userId,
        int tabType,
        int count,
        string cursor,
        CancellationToken cancellationToken
    )
    {
        AppUserProfilePayload payload = new()
        {
            Count = count,
            Cursor = cursor,
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
        int count,
        int offset,
        CancellationToken cancellationToken
    )
    {
        AppResponse<AppUserShelfData> response = await _appTransport
            .GetAsync<AppUserShelfData>(
                "/reading/bookapi/bookshelf/video/info/v",
                [
                    new("count", count.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                    new(
                        "offset",
                        offset.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    ),
                    new("target_user_id", userId),
                ],
                cancellationToken
            )
            .ConfigureAwait(false);
        return response.Data;
    }

    private async Task<AppUserActionData?> LoadLikesAsync(
        string userId,
        int count,
        int offset,
        CancellationToken cancellationToken
    )
    {
        AppResponse<AppUserActionData> response = await _appTransport
            .GetAsync<AppUserActionData>(
                "/reading/ugc/action/mget/v",
                [
                    new("count", count.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                    new("object_type_list", "6,15,10"),
                    new("target_user_id", userId),
                    new(
                        "offset",
                        offset.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    ),
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

            string id = record.Video.Id.Trim();
            if (!ValueNormalizer.IsNumericId(id) || !seen.Add(id))
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
            string id = video.Id.Trim();
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

            string id = ValueNormalizer.First(item.Video.SeriesId, item.Video.VideoId).Trim();
            if (!ValueNormalizer.IsNumericId(id) || !seen.Add(id))
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
            string id = ValueNormalizer.First(series.SeriesId, video.SeriesId).Trim();
            if (!ValueNormalizer.IsNumericId(id) || !seen.Add(id))
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

    private static UserPageInfo ProfilePageInfo(
        AppUserProfileData? data,
        AppUserProfileData metadata,
        int tabType
    ) =>
        new()
        {
            Cursor = data?.ListInfo?.Cursor ?? string.Empty,
            HasMore = data?.ListInfo?.HasMore ?? false,
            Total = TabTotal(data, tabType, TabTotal(metadata, tabType)),
        };

    private static long TabTotal(AppUserProfileData? data, int tabType, long fallback = 0) =>
        (data?.Tabs ?? [])
            .Where(static tab => tab is not null)
            .FirstOrDefault(tab => tab.Type == tabType)
            ?.Total
        ?? fallback;

    private static void ValidateCount(int count)
    {
        if (count is < 1 or > MaxPageSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(count),
                $"账号主页分页数量必须在 1 到 {MaxPageSize} 之间。"
            );
        }
    }

    private static void ValidateOffset(int offset)
    {
        if (offset is < 0 or > 1_000_000)
        {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }
    }

    private static string NormalizeCursor(string value)
    {
        value = (value ?? string.Empty).Trim();
        if (value.Length > 8192 || value.Any(static c => c is '\r' or '\n' or '\0'))
        {
            throw new ArgumentException("账号主页游标无效。", nameof(value));
        }

        return value;
    }

    private static string NormalizeUserId(string value)
    {
        value = (value ?? string.Empty).Trim();
        if (!ValueNormalizer.IsNumericId(value))
        {
            throw new ArgumentException("账号 ID 无效。", nameof(value));
        }

        return value;
    }
}
