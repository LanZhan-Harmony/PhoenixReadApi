using PhoenixReadApi.Internal.Contracts.App;
using PhoenixReadApi.Internal.Contracts.App.Comment;
using PhoenixReadApi.Internal.Contracts.App.Users;
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

    internal async Task<UserWorkPage> GetWorksAsync(
        string userId,
        UserWorksQuery query,
        CancellationToken cancellationToken
    )
    {
        userId = NormalizeUserId(userId);
        ArgumentNullException.ThrowIfNull(query);
        ValidateCount(query.Count);
        string cursor = NormalizeCursor(query.Cursor);
        string sort = query.Sort == UserWorkSort.Hot ? "hot" : "time_desc";
        int? subTab = query.SubTab == UserWorkSubTab.All ? null : (int)query.SubTab;

        AppUserProfilePayload payload = new()
        {
            Count = query.Count,
            Cursor = cursor,
            TabType = 16,
            SubTabType = subTab,
            Sort = sort,
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

        AppUserProfileData? data = response.Data;
        IReadOnlyList<Drama> items = MapWorks(data);
        return new UserWorkPage
        {
            Items = items,
            Cursor = data?.ListInfo?.Cursor ?? string.Empty,
            HasMore = data?.ListInfo?.HasMore ?? false,
            Total = TabTotal(data, 16),
        };
    }

    internal Task<UserRelationPage> GetFollowersAsync(
        string userId,
        string cursor,
        int count,
        CancellationToken cancellationToken
    ) => LoadRelationListAsync(userId, 1508, cursor, count, cancellationToken);

    internal Task<UserRelationPage> GetFollowingsAsync(
        string userId,
        string cursor,
        int count,
        CancellationToken cancellationToken
    ) => LoadRelationListAsync(userId, 1507, cursor, count, cancellationToken);

    private async Task<UserRelationPage> LoadRelationListAsync(
        string userId,
        int commentSource,
        string cursor,
        int count,
        CancellationToken cancellationToken
    )
    {
        userId = NormalizeUserId(userId);
        ValidateCount(count);
        cursor = NormalizeCursor(cursor);

        AppUserRelationListPayload payload = new()
        {
            Count = count,
            CommentSource = commentSource,
            TargetUserId = userId,
            Cursor = cursor.Length > 0 ? cursor : null,
        };

        AppResponse<AppUserRelationListData> response = await _appTransport
            .PostAsync<AppUserRelationListPayload, AppUserRelationListData>(
                "/novel/commentapi/relation/list/" + userId + "/v1",
                payload,
                commentRequest: true,
                cancellationToken: cancellationToken,
                commentSource: commentSource,
                serverChannel: null
            )
            .ConfigureAwait(false);

        AppUserRelationListData? data = response.Data;
        List<UserRelationItem> items = [];
        HashSet<string> seen = new(StringComparer.Ordinal);

        foreach (AppUserRelationUgcUser? ugc in data?.Users ?? [])
        {
            if (ugc is null)
            {
                continue;
            }

            string id = ValueNormalizer.First(ugc.UserId, ugc.BaseInfo?.UserId).Trim();
            if (!ValueNormalizer.IsNumericId(id) || !seen.Add(id))
            {
                continue;
            }

            AppUgcUserBaseInfo baseInfo = ugc.BaseInfo ?? new();
            AppUgcUserRelation relation = ugc.UserRelation ?? new();
            AppUgcUserTag tag = ugc.UserTag ?? new();

            IReadOnlyList<string> roles = (tag.SubTitleList ?? [])
                .Where(static s => s is not null && !string.IsNullOrWhiteSpace(s.Text))
                .Select(static s => s.Text.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToList();

            bool isCreator = (tag.CreatorTypesFull?.Count ?? 0) > 0 || roles.Count > 0;

            items.Add(
                new UserRelationItem
                {
                    User = new User
                    {
                        Id = id,
                        Name = ValueNormalizer.First(baseInfo.UserName, id),
                        Introduction = baseInfo.Description ?? string.Empty,
                        AvatarUrl = baseInfo.UserAvatar,
                    },
                    Gender = baseInfo.Gender,
                    IsVerified = tag.IsBlueVip,
                    IsCreator = isCreator,
                    CreatorRoles = roles,
                    CanFollow = relation.CanFollow,
                    IsFollowing = relation.RelationType is 2 or 4,
                }
            );
        }

        return new UserRelationPage
        {
            Items = items,
            Cursor = data?.ListInfo?.Cursor ?? string.Empty,
            HasMore = data?.ListInfo?.HasMore ?? false,
            IsPrivacyProtected = data?.IsPrivacyOn ?? false,
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
        Task<AppResponse<AppUserProfileGetResponse>?> ugcTask = LoadUgcProfileAsync(user.Id, cancellationToken);

        AppUserProfilePayload metadataPayload = new()
        {
            Count = 0,
            TabType = 0,
            TargetUserId = user.Id,
        };
        Task<AppResponse<AppUserProfileData>> metadataTask = _appTransport
            .PostAsync<AppUserProfilePayload, AppUserProfileData>(
                "/novel/commentapi/user/profile/" + user.Id + "/v1",
                metadataPayload,
                commentRequest: true,
                cancellationToken,
                commentSource: 1506,
                serverChannel: null
            );

        await Task.WhenAll(ugcTask, metadataTask).ConfigureAwait(false);

        AppResponse<AppUserProfileGetResponse>? ugcResponse = await ugcTask.ConfigureAwait(false);
        AppResponse<AppUserProfileData> metadataResponse = await metadataTask.ConfigureAwait(false);

        AppUserProfileData metadata =
            metadataResponse.Data
            ?? throw new PhoenixReadApiResponseException(
                "账号主页未返回有效数据。",
                "user-profile"
            );

        AppUgcUser? ugc = ugcResponse?.Data?.UgcUser;

        HashSet<int> tabTypes =
        [
            .. (metadata.Tabs ?? [])
                .Where(static tab => tab is not null)
                .Select(static tab => tab.Type),
        ];

        // If tabs has 16 (Works - 短剧作品), load Tab 16; else if 8 (Related), load Tab 8
        int worksTabType = tabTypes.Contains(16) ? 16 : (tabTypes.Contains(8) ? 8 : 0);
        Task<AppUserProfileData?> worksTask = worksTabType != 0
            ? LoadProfileTabAsync(user.Id, worksTabType, PageSize, string.Empty, cancellationToken)
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

        await Task.WhenAll(worksTask, activitiesTask, collectionsTask, likesTask)
            .ConfigureAwait(false);

        AppUserProfileData? worksData = await worksTask.ConfigureAwait(false);
        AppUserProfileData? activities = await activitiesTask.ConfigureAwait(false);
        AppUserShelfData? collections = await collectionsTask.ConfigureAwait(false);
        AppUserActionData? likes = await likesTask.ConfigureAwait(false);

        AppCommentUserInfo? userInfo = metadata.UserInfo;
        string resolvedName = ValueNormalizer.First(
            user.Name,
            ugc?.BaseInfo?.UserName,
            userInfo?.Name,
            userInfo?.BaseInfo?.Name
        );
        string resolvedAvatar = ValueNormalizer.First(
            user.AvatarUrl,
            ugc?.BaseInfo?.UserAvatar,
            userInfo?.AvatarUrl,
            userInfo?.BaseInfo?.AvatarUrl
        );
        string resolvedIntro = ValueNormalizer.First(
            user.Introduction,
            ugc?.BaseInfo?.Description,
            userInfo?.Description,
            userInfo?.Introduction,
            userInfo?.BaseInfo?.Description,
            userInfo?.BaseInfo?.Introduction
        );

        IReadOnlyList<string> actorRoles = ugc?.UserTag?.SubTitleList?
            .Select(static s => s.Text)
            .Where(static s => !string.IsNullOrWhiteSpace(s))
            .ToList() ?? [];

        IReadOnlyList<string> badges = ugc?.UserTag?.UserTitleInfo?
            .Select(static t => t.ZhTitle)
            .Where(static s => !string.IsNullOrWhiteSpace(s))
            .ToList() ?? [];

        IReadOnlyList<UserHonor> honors = ugc?.UserTag?.HonorTagBriefInfo?.HonorTagDetailList?
            .Select(static h => new UserHonor
            {
                Name = h.Name,
                IconUrl = h.ImgUrl,
                SmallIconUrl = h.SmallImgUrl,
            })
            .ToList() ?? [];

        bool isBlueVip = ugc?.UserTag?.IsBlueVip ?? false;
        bool isActor = isBlueVip
            || actorRoles.Any(static r => r.Contains("演员", StringComparison.OrdinalIgnoreCase))
            || tabTypes.Contains(16);

        IReadOnlyList<Drama> works = MapWorks(worksData);
        long worksTotal = TabTotal(metadata, 16);
        if (worksTotal == 0)
        {
            worksTotal = TabTotal(metadata, 8);
        }

        string bgCover = ValueNormalizer.First(ugc?.BgCover, ugcResponse?.Data?.BgCover);

        IReadOnlyList<UserProfileTab> availableTabs = (metadata.Tabs ?? [])
            .Where(static t => t is not null)
            .Select(static t => new UserProfileTab
            {
                Type = t.Type,
                Name = t.Name,
                Total = t.Total,
            })
            .ToList();

        return new UserProfile
        {
            User = new User
            {
                Id = user.Id,
                Name = resolvedName,
                Introduction = resolvedIntro,
                AvatarUrl = resolvedAvatar,
            },
            HongguoId = ugc?.BaseInfo?.BizUserId ?? string.Empty,
            IsVerified = isBlueVip,
            IsCreator = isActor,
            CreatorRoles = actorRoles,
            Badges = badges,
            Honors = honors,
            FansCount = Math.Max(0, ugc?.UserRelation?.FansNum ?? 0),
            FollowCount = Math.Max(0, ugc?.UserRelation?.FollowUserNum ?? 0),
            LikedCount = Math.Max(0, ugc?.UserRelation?.RecvDiggedCount ?? 0),
            CanFollow = ugc?.UserRelation?.CanFollow ?? true,
            IsFollowing = ugc?.UserRelation?.RelationType is 2 or 4,
            BackgroundCoverUrl = bgCover,
            Works = works,
            WorksPage = new UserWorkPage
            {
                Items = works,
                Cursor = worksData?.ListInfo?.Cursor ?? string.Empty,
                HasMore = worksData?.ListInfo?.HasMore ?? false,
                Total = worksTotal,
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
            AvailableTabs = availableTabs,
        };
    }

    private async Task<AppResponse<AppUserProfileGetResponse>?> LoadUgcProfileAsync(
        string userId,
        CancellationToken cancellationToken
    )
    {
        try
        {
            return await _appTransport
                .GetAsync<AppUserProfileGetResponse>(
                    "/reading/user/profile/get/v1/",
                    [
                        new("user_id", userId),
                        new("target_user_id", userId),
                    ],
                    cancellationToken
                )
                .ConfigureAwait(false);
        }
        catch
        {
            // 非致命降级：主账号资料接口若未返回，不阻断主页其余内容呈现
            return null;
        }
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

    private static IReadOnlyList<Drama> MapWorks(AppUserProfileData? data)
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
            IReadOnlyList<string> tags = record.Video.Expand?.SubTitleList?
                .Select(static s => s.Text)
                .Where(static s => !string.IsNullOrWhiteSpace(s))
                .ToList() ?? [];

            long hot = record.Video.Stat?.Hot ?? 0;
            long viewCount = record.Video.Stat?.ViewCount ?? 0;
            long followCount = record.Video.Stat?.FollowedCount ?? 0;

            result.Add(
                new Drama
                {
                    Id = id,
                    Title = ValueNormalizer.First(common.Title, id),
                    Description = common.Introduction,
                    CoverUrl = common.CoverUrl,
                    EpisodeCount = Math.Max(0, record.Video.Expand?.EpisodeCount ?? 0),
                    Tags = tags,
                    Heat = hot > 0 ? ValueNormalizer.FormatCount(hot) + "热度" : string.Empty,
                    Views = viewCount > 0 ? ValueNormalizer.FormatCount(viewCount) : string.Empty,
                    PlayCount = Math.Max(0, viewCount),
                    FollowCount = Math.Max(0, followCount),
                }
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
            Total = TabTotal(metadata, tabType),
        };

    private static long TabTotal(AppUserProfileData? data, int tabType)
    {
        if (data?.Tabs is null)
        {
            return 0;
        }

        foreach (AppUserProfileTab? tab in data.Tabs)
        {
            if (tab is not null && tab.Type == tabType)
            {
                return Math.Max(0, tab.Total);
            }
        }

        return 0;
    }

    private static string NormalizeUserId(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("账号 ID 不能为空。", nameof(userId));
        }

        string trimmed = userId.Trim();
        if (!ValueNormalizer.IsNumericId(trimmed))
        {
            throw new ArgumentException("账号 ID 必须是由数字构成的字符串。", nameof(userId));
        }

        return trimmed;
    }

    private static void ValidateCount(int count)
    {
        if (count is < 1 or > MaxPageSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(count),
                count,
                "分页数量必须在 1 到 " + MaxPageSize + " 之间。"
            );
        }
    }

    private static void ValidateOffset(int offset)
    {
        if (offset < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(offset),
                offset,
                "分页偏移量不能为负数。"
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
