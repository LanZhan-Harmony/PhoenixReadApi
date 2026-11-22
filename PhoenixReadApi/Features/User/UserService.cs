using System.Globalization;
using PhoenixReadApi.Internal.Contracts.App;
using PhoenixReadApi.Internal.Contracts.App.Comment;
using PhoenixReadApi.Internal.Contracts.App.User;
using PhoenixReadApi.Internal.Features.Circle;
using PhoenixReadApi.Internal.Features.Shared;
using PhoenixReadApi.Internal.Infrastructure.Runtime;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Features.User;

using Drama = PhoenixReadApi.Drama;
using User = PhoenixReadApi.User;
using UserReview = PhoenixReadApi.UserReview;
using UserWork = PhoenixReadApi.UserWork;

/// <summary>加载任意公开账号的主页及其公开内容。</summary>
internal sealed class UserService : PhoenixFeatureService
{
    private const int PageSize = 18;
    private const int MaxPageSize = 50;
    private readonly CircleService _circleService;

    internal UserService(PhoenixReadClientRuntime runtime, CircleService circleService)
        : base(runtime)
    {
        _circleService = circleService ?? throw new ArgumentNullException(nameof(circleService));
    }

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
            TabType = 23,
            SubTabType = subTab,
            Sort = sort,
            TargetUserId = userId,
            ExtraParam = new Dictionary<string, string> { ["is_video_tab_comment"] = "1" },
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
        if (
            (data?.Items is null || data.Items.Count == 0)
            && subTab is null
            && string.IsNullOrEmpty(cursor)
        )
        {
            AppUserProfilePayload fallbackPayload = new()
            {
                Count = query.Count,
                Cursor = cursor,
                TabType = 16,
                SubTabType = null,
                Sort = sort,
                TargetUserId = userId,
            };
            AppResponse<AppUserProfileData> fallbackResp = await _appTransport
                .PostAsync<AppUserProfilePayload, AppUserProfileData>(
                    "/novel/commentapi/user/profile/" + userId + "/v1",
                    fallbackPayload,
                    commentRequest: true,
                    cancellationToken,
                    commentSource: 1506,
                    serverChannel: null
                )
                .ConfigureAwait(false);
            if (fallbackResp.Data?.Items is { Count: > 0 })
            {
                data = fallbackResp.Data;
            }
        }

        IReadOnlyList<UserWork> items = MapWorks(data);
        long total = subTab.HasValue ? SubTabTotal(data, subTab.Value) : TabTotal(data, 23);
        if (total == 0)
        {
            total = TabTotal(data, 16);
        }
        if (total == 0)
        {
            total = TabTotal(data, 8);
        }

        return new UserWorkPage
        {
            Items = items,
            Cursor = data?.ListInfo?.Cursor ?? string.Empty,
            HasMore = data?.ListInfo?.HasMore ?? false,
            Total = Math.Max(total, items.Count),
        };
    }

    internal async Task<DramaPage> GetFavoritesAsync(
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
        return new DramaPage
        {
            Items = MapFavorites(data),
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
        Task<AppResponse<AppUserProfileGetResponse>?> ugcTask = LoadUgcProfileAsync(
            user.Id,
            cancellationToken
        );

        AppUserProfilePayload metadataPayload = new()
        {
            Count = 0,
            TabType = 0,
            TargetUserId = user.Id,
        };
        Task<AppResponse<AppUserProfileData>> metadataTask = _appTransport.PostAsync<
            AppUserProfilePayload,
            AppUserProfileData
        >(
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

        AppUserProfileData metadata = metadataResponse.Data
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

        // If tabs has 23 or 16 or 8 (Works - 短剧作品), or tab name contains "作品", load that tab
        int worksTabType = 0;
        foreach (AppUserProfileTab? tab in metadata.Tabs ?? [])
        {
            if (
                tab is not null
                && (tab.Type is 23 or 16 or 8 || tab.Name?.Contains("作品") == true)
            )
            {
                worksTabType = tab.Type;
                break;
            }
        }
        Task<AppUserProfileData?> worksTask =
            worksTabType != 0
                ? LoadProfileTabAsync(
                    user.Id,
                    worksTabType,
                    PageSize,
                    string.Empty,
                    cancellationToken
                )
                : Task.FromResult<AppUserProfileData?>(null);

        Task<CirclePage> circleTask = tabTypes.Contains((int)UserProfileTabType.Circle)
            ? _circleService.GetUserCircleAsync(user.Id, PageSize, string.Empty, cancellationToken)
            : Task.FromResult(new CirclePage());
        Task<AppUserShelfData?> collectionsTask = tabTypes.Contains((int)UserProfileTabType.Favorite)
            ? LoadCollectionsAsync(user.Id, PageSize, 0, cancellationToken)
            : Task.FromResult<AppUserShelfData?>(null);
        Task<AppUserActionData?> likesTask = tabTypes.Contains((int)UserProfileTabType.Like)
            ? LoadLikesAsync(user.Id, PageSize, 0, cancellationToken)
            : Task.FromResult<AppUserActionData?>(null);

        await Task.WhenAll(worksTask, circleTask, collectionsTask, likesTask).ConfigureAwait(false);

        AppUserProfileData? worksData = await worksTask.ConfigureAwait(false);
        CirclePage circlePage = await circleTask.ConfigureAwait(false);
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

        IReadOnlyList<string> actorRoles = ugc?.UserTag?.SubTitleList?.Select(static s => s.Text)
            .Where(static s => !string.IsNullOrWhiteSpace(s))
            .ToList()
            ?? [];

        IReadOnlyList<string> badges = ugc?.UserTag?.UserTitleInfo?.Select(static t => t.ZhTitle)
            .Where(static s => !string.IsNullOrWhiteSpace(s))
            .ToList()
            ?? [];

        IReadOnlyList<UserHonor> honors =
            ugc?.UserTag?.HonorTagBriefInfo?.HonorTagDetailList?.Select(static h => new UserHonor
                {
                    Name = h.Name,
                    IconUrl = h.ImgUrl,
                    SmallIconUrl = h.SmallImgUrl,
                })
                .ToList()
            ?? [];

        bool isBlueVip = ugc?.UserTag?.IsBlueVip ?? false;
        bool isActor =
            isBlueVip
            || actorRoles.Any(static r => r.Contains("演员", StringComparison.OrdinalIgnoreCase))
            || worksTabType != 0;

        IReadOnlyList<UserWork> works = MapWorks(worksData);
        long worksTotal = worksTabType != 0 ? TabTotal(metadata, worksTabType) : 0;

        string bgCover = ValueNormalizer.First(ugc?.BgCover, ugcResponse?.Data?.BgCover);

        IReadOnlyList<UserProfileTab> availableTabs =
        [
            .. (metadata.Tabs ?? [])
                .Where(static t => t is not null)
                .Select(static t => new UserProfileTab
                {
                    Type = ResolveTabType(t),
                    Name = t.Name,
                    Total = t.Total,
                }),
        ];

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
            LikeCount = Math.Max(0, ugc?.UserRelation?.RecvDiggedCount ?? 0),
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
            Collections = MapFavorites(collections),
            CollectionsPage = new UserPageInfo
            {
                NextOffset = collections?.NextOffset ?? 0,
                HasMore = collections?.HasMore ?? false,
                Total = Math.Max(collections?.Total ?? 0, TabTotal(metadata, (int)UserProfileTabType.Favorite)),
            },
            Likes = MapLikes(likes),
            LikesPage = new UserPageInfo
            {
                NextOffset = likes?.NextOffset ?? 0,
                HasMore = likes?.HasMore ?? false,
                Total = Math.Max(likes?.Total ?? 0, TabTotal(metadata, (int)UserProfileTabType.Like)),
            },
            CircleItems = circlePage.Items,
            CirclePage = circlePage,
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
                    [new("user_id", userId), new("target_user_id", userId)],
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

    private static IReadOnlyList<UserWork> MapWorks(AppUserProfileData? data)
    {
        if (data is null)
        {
            return [];
        }

        List<UserWork> result = [];
        HashSet<string> seen = [with(StringComparer.Ordinal)];
        foreach (AppUserWorkRecord? record in data.Items ?? [])
        {
            if (record is null)
            {
                continue;
            }

            if (record.Video is not null && !string.IsNullOrWhiteSpace(record.Video.Id))
            {
                string id = record.Video.Id.Trim();
                if (!ValueNormalizer.IsNumericId(id) || !seen.Add("v:" + id))
                {
                    continue;
                }

                AppUserVideoCommon common = record.Video.Common;
                IReadOnlyList<string> tags = record
                    .Video.Expand?.SubTitleList?.Select(static s => s.Text)
                    .Where(static s => !string.IsNullOrWhiteSpace(s))
                    .ToList()
                    ?? [];

                long hot = record.Video.Stat?.Hot ?? 0;
                long viewCount = record.Video.Stat?.ViewCount ?? 0;
                long followCount = record.Video.Stat?.FollowedCount ?? 0;

                Drama drama = new Drama
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
                };

                result.Add(drama);
                continue;
            }

            if (record.Comment is not null && !string.IsNullOrWhiteSpace(record.Comment.Id))
            {
                string id = record.Comment.Id.Trim();
                if (!ValueNormalizer.IsNumericId(id) || !seen.Add("c:" + id))
                {
                    continue;
                }

                AppComment comment = record.Comment;
                AppCommentExpand expand = comment.Expand ?? new();
                AppCommentStat stat = comment.Stat ?? new();
                AppCommentExpandBookInfo bookInfo = expand.BookInfo ?? new();

                string dramaId = ValueNormalizer.First(
                    bookInfo.BookId,
                    expand.BookId,
                    comment.Common?.GroupId
                );
                IReadOnlyList<string> dramaTags = !string.IsNullOrWhiteSpace(bookInfo.Tags)
                    ?
                    [
                        .. bookInfo.Tags.Split(
                            ',',
                            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
                        ),
                    ]
                    : [];

                Drama reviewedDrama = new Drama
                {
                    Id = dramaId,
                    Title = ValueNormalizer.First(bookInfo.BookName, dramaId),
                    Description = bookInfo.Abstract,
                    CoverUrl = bookInfo.ThumbUrl,
                    EpisodeCount = Math.Max(0, bookInfo.SerialCount),
                    Tags = dramaTags,
                };

                UserReview userReview = new UserReview
                {
                    Id = id,
                    Drama = reviewedDrama,
                    Content = comment.Common?.Content?.Text ?? string.Empty,
                    Rating = expand.Score,
                    RatingText = expand.ScoreSuffixText,
                    LikeCount = Math.Max(0, stat.LikeCount),
                    ReplyCount = Math.Max(0, stat.ReplyCount),
                    CreatedAtUnixSeconds = comment.Common?.CreatedAt ?? 0,
                };

                result.Add(userReview);
                continue;
            }

            if (
                record.SubscribeItem is not null
                && !string.IsNullOrWhiteSpace(record.SubscribeItem.ItemId)
            )
            {
                string id = record.SubscribeItem.ItemId.Trim();
                if (!ValueNormalizer.IsNumericId(id) || !seen.Add("s:" + id))
                {
                    continue;
                }

                AppUserSubscribeItem sub = record.SubscribeItem;
                List<string> tags = [];
                long followCount = 0;
                foreach (AppUserSubTitleItem? item in sub.SubTitleList ?? [])
                {
                    if (item is null || string.IsNullOrWhiteSpace(item.Text))
                    {
                        continue;
                    }

                    string text = item.Text.Trim();
                    if (text.EndsWith("人预约", StringComparison.Ordinal))
                    {
                        string countPart = text[..^3].Trim();
                        if (
                            countPart.EndsWith("万", StringComparison.Ordinal)
                            && double.TryParse(
                                countPart[..^1],
                                CultureInfo.InvariantCulture,
                                out double wan
                            )
                        )
                        {
                            followCount = (long)(wan * 10_000);
                        }
                        else if (
                            long.TryParse(countPart, CultureInfo.InvariantCulture, out long num)
                        )
                        {
                            followCount = num;
                        }
                    }
                    else
                    {
                        tags.Add(text);
                    }
                }

                string onlineDate = sub.SubInfo;
                if (string.IsNullOrWhiteSpace(onlineDate) && sub.SchedulePublishTime > 0)
                {
                    onlineDate = DateTimeOffset
                        .FromUnixTimeSeconds(sub.SchedulePublishTime)
                        .ToOffset(TimeSpan.FromHours(8))
                        .ToString("yyyy-MM-dd");
                }

                Drama drama = new Drama
                {
                    Id = id,
                    Title = ValueNormalizer.First(sub.Name, id),
                    CoverUrl = sub.CoverUrl,
                    ReleaseStatus = DramaReleaseStatus.Upcoming,
                    OnlineDate = onlineDate,
                    Remark = sub.SubInfo,
                    Tags = tags,
                    FollowCount = followCount,
                };

                result.Add(drama);
            }
        }

        return result;
    }

    private static IReadOnlyList<Drama> MapFavorites(AppUserShelfData? data)
    {
        if (data is null)
        {
            return [];
        }

        List<Drama> result = [];
        HashSet<string> seen = [with(StringComparer.Ordinal)];
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
        HashSet<string> seen = [with(StringComparer.Ordinal)];
        foreach (AppUserActionRecord? record in data.Items ?? [])
        {
            if (record?.Video is null)
            {
                continue;
            }

            AppUserActionVideoData video = record.Video;
            AppUserActionSeriesDetail drama = video.Series;
            string id = ValueNormalizer.First(drama.SeriesId, video.SeriesId).Trim();
            if (!ValueNormalizer.IsNumericId(id) || !seen.Add(id))
            {
                continue;
            }

            result.Add(
                DramaMapper.FromAction(
                    id,
                    drama.Title,
                    drama.Introduction.Length > 0 ? drama.Introduction : video.Description,
                    drama.CoverUrl.Length > 0 ? drama.CoverUrl : video.CoverUrl,
                    Math.Max(drama.EpisodeCount, video.EpisodeCount),
                    drama.Status,
                    drama.Tags
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

    private static long SubTabTotal(AppUserProfileData? data, int subTabType)
    {
        if (data is null)
        {
            return 0;
        }

        if (data.Highlight?.SubTabs is not null)
        {
            foreach (AppUserProfileSubTab? sub in data.Highlight.SubTabs)
            {
                if (sub is not null && sub.Type == subTabType)
                {
                    return Math.Max(0, sub.Total);
                }
            }
        }

        if (data.Tabs is not null)
        {
            foreach (AppUserProfileTab? tab in data.Tabs)
            {
                if (tab?.SubTabs is null)
                {
                    continue;
                }

                foreach (AppUserProfileSubTab? sub in tab.SubTabs)
                {
                    if (sub is not null && sub.Type == subTabType)
                    {
                        return Math.Max(0, sub.Total);
                    }
                }
            }
        }

        return 0;
    }

    private static UserProfileTabType ResolveTabType(AppUserProfileTab tab)
    {
        if (
            tab.Type is 23 or 16 or 8
            || (!string.IsNullOrWhiteSpace(tab.Name) && tab.Name.Contains("作品", StringComparison.OrdinalIgnoreCase))
        )
        {
            return UserProfileTabType.Work;
        }

        if (
            tab.Type == (int)UserProfileTabType.Circle
            || (!string.IsNullOrWhiteSpace(tab.Name) && tab.Name.Contains("圈", StringComparison.OrdinalIgnoreCase))
        )
        {
            return UserProfileTabType.Circle;
        }

        if (
            tab.Type == (int)UserProfileTabType.Favorite
            || (!string.IsNullOrWhiteSpace(tab.Name) && tab.Name.Contains("收藏", StringComparison.OrdinalIgnoreCase))
        )
        {
            return UserProfileTabType.Favorite;
        }

        if (
            tab.Type == (int)UserProfileTabType.Like
            || (!string.IsNullOrWhiteSpace(tab.Name) && tab.Name.Contains("赞", StringComparison.OrdinalIgnoreCase))
        )
        {
            return UserProfileTabType.Like;
        }

        return Enum.IsDefined(typeof(UserProfileTabType), tab.Type)
            ? (UserProfileTabType)tab.Type
            : UserProfileTabType.Unknown;
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
            throw new ArgumentOutOfRangeException(nameof(offset), offset, "分页偏移量不能为负数。");
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
