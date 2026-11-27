using System.Text;
using System.Text.Json;
using PhoenixReadApi.Internal.Contracts.App;
using PhoenixReadApi.Internal.Contracts.App.Circle;
using PhoenixReadApi.Internal.Contracts.App.Comment;
using PhoenixReadApi.Internal.Features.Shared;
using PhoenixReadApi.Internal.Infrastructure.Runtime;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Features.Circle;

using Drama = PhoenixReadApi.Drama;
using User = PhoenixReadApi.User;

/// <summary>红果圈（演职人员/创作者粉丝圈混合流、图文详情与短视频详情）业务服务。</summary>
internal sealed class CircleService : PhoenixFeatureService
{
    private const int MaxPageSize = 50;

    internal CircleService(PhoenixReadClientRuntime runtime)
        : base(runtime) { }

    /// <summary>获取指定用户/演职人员的红果圈 Feed 混合流。</summary>
    internal async Task<CirclePage> GetUserCircleAsync(
        string userId,
        string cursor,
        int count,
        CancellationToken cancellationToken
    )
    {
        userId = NormalizeUserId(userId);
        ValidateCount(count);
        cursor = NormalizeCursor(cursor);

        AppCircleFeedPayload payload = new()
        {
            Count = count,
            Cursor = cursor,
            TabType = 7,
            TargetUserId = userId,
            CommentSource = 1506,
            Sort = "smart_hot_desc",
            ProfileUserType = 0,
        };

        AppResponse<AppCircleFeedData> response = await _appTransport
            .PostAsync<AppCircleFeedPayload, AppCircleFeedData>(
                "/novel/commentapi/user/profile/" + userId + "/v1",
                payload,
                commentRequest: true,
                cancellationToken,
                commentSource: 1506
            )
            .ConfigureAwait(false);

        AppCircleFeedData data = response.Data ?? new();
        List<CircleItem> items = [];
        HashSet<string> seen = [with(StringComparer.Ordinal)];

        foreach (AppCircleFeedItem? record in data.Items ?? [])
        {
            if (record is null)
            {
                continue;
            }

            if (record.DataType == 1 && record.Video is not null)
            {
                ShortVideo video = MapFeedVideoItem(record.Video);
                if (video.Id.Length > 0 && seen.Add("v:" + video.Id))
                {
                    items.Add(video);
                }
            }
            else if (record.DataType == 3 && record.Post is not null)
            {
                Post post = MapFeedPostItem(record.Post);
                if (post.Id.Length > 0 && seen.Add("p:" + post.Id))
                {
                    items.Add(post);
                }
            }
        }

        return new CirclePage
        {
            Items = items,
            Cursor = data.ListInfo?.Cursor ?? string.Empty,
            HasMore = data.ListInfo?.HasMore ?? false,
            Total = TabTotal(data.Tabs, 7),
        };
    }

    /// <summary>获取指定图文帖子的完整正文、高清多图与关联短剧。</summary>
    internal async Task<Post> GetCirclePostAsync(
        string postId,
        CancellationToken cancellationToken
    )
    {
        postId = NormalizeNumericId(postId, nameof(postId));

        AppCirclePostDetailPayload payload = new() { GroupIds = [postId], GroupType = 3 };

        AppResponse<AppCirclePostData> response = await _appTransport
            .PostAsync<AppCirclePostDetailPayload, AppCirclePostData>(
                "/novel/articleapi/post_data/multi_get/v1/",
                payload,
                commentRequest: false,
                cancellationToken
            )
            .ConfigureAwait(false);

        AppCirclePostItem post = response.Data?.Posts.FirstOrDefault(x => x.PostId == postId)
            ?? response.Data?.Posts.FirstOrDefault()
            ?? throw new PhoenixReadApiResponseException(
                "未找到指定的红果圈图文内容。",
                "circle-post"
            );

        return MapCirclePost(post);
    }

    /// <summary>获取指定短视频的完整元数据、演员阵容与关联短剧。</summary>
    internal async Task<ShortVideo> GetCircleVideoAsync(
        string videoId,
        CancellationToken cancellationToken
    )
    {
        videoId = NormalizeNumericId(videoId, nameof(videoId));

        AppCircleVideoDetailPayload payload = new()
        {
            SeriesId = videoId,
            BizParam = new AppCircleVideoBizParam
            {
                DetailPageVersion = 1,
                DisableDiggStat = false,
                DisableVideoRelateBook = false,
                Source = 5,
                VideoIdType = 1,
            },
        };

        AppResponse<AppCircleVideoDetailData> response = await _appTransport
            .PostAsync<AppCircleVideoDetailPayload, AppCircleVideoDetailData>(
                "/novel/player/video_detail/v1/",
                payload,
                commentRequest: false,
                cancellationToken
            )
            .ConfigureAwait(false);

        AppCircleVideoDetailDataContainer container = response.Data?.VideoData
            ?? throw new PhoenixReadApiResponseException(
                "未找到指定的短视频内容。",
                "circle-video"
            );

        AppCircleVideoSubDetail? subDetail = container.VideoDetailList.FirstOrDefault();
        string resolvedId = ValueNormalizer.First(container.SeriesId, videoId);
        List<Drama> relatedDramas =
            subDetail is not null && subDetail.SeriesId.Length > 0
                ?
                [
                    new Drama
                    {
                        Id = subDetail.SeriesId,
                        Title = subDetail.Title,
                        CoverUrl = subDetail.CoverUrl,
                        Description = subDetail.Introduction,
                        EpisodeCount = Math.Max(0, subDetail.EpisodeCount),
                    },
                ]
                : [];

        return new ShortVideo
        {
            Id = resolvedId,
            VideoId = resolvedId,
            Title = ValueNormalizer.First(container.Title, subDetail?.Title, string.Empty),
            Description = ValueNormalizer.First(
                container.Introduction,
                subDetail?.Introduction,
                string.Empty
            ),
            CoverUrl = ValueNormalizer.First(container.CoverUrl, subDetail?.CoverUrl, string.Empty),
            DurationSeconds = (int)Math.Max(0, container.DurationSeconds),
            ViewCount = Math.Max(0, Math.Max(container.PlayCount, subDetail?.PlayCount ?? 0)),
            LikeCount = Math.Max(0, container.LikeCount),
            CommentCount = Math.Max(0, container.FollowedCount),
            CreatedAtUnixSeconds = Math.Max(0, container.CreatedAt),
            Author = MapCommentUserInfo(container.UgcUserInfo),
            Credits = DramaMapper.MapCredits(subDetail?.Celebrities ?? []),
            RelatedDramas = relatedDramas,
        };
    }

    private static ShortVideo MapFeedVideoItem(AppCircleVideoItem video)
    {
        AppCircleVideoCommon common = video.Common;
        AppCircleVideoStat stat = video.Stat;
        AppCircleRelatedDrama? relatedDrama = video.Expand.Videos.FirstOrDefault();
        string id = ValueNormalizer.First(video.VideoId, string.Empty);

        return new ShortVideo
        {
            Id = id,
            VideoId = id,
            Title = ValueNormalizer.First(common.Title, string.Empty),
            Description = ValueNormalizer.First(common.Introduction, string.Empty),
            CoverUrl = ValueNormalizer.First(common.CoverUrl, string.Empty),
            Author = MapCommentUserInfo(common.UserInfo),
            CreatedAtUnixSeconds = Math.Max(0, common.CreatedAt),
            LikeCount = Math.Max(0, stat.DiggCount),
            CommentCount = Math.Max(0, stat.FollowedCount),
            ViewCount = Math.Max(0, stat.PlayCount),
            RelatedDramas =
                relatedDrama is not null && relatedDrama.SeriesId.Length > 0
                    ?
                    [
                        new Drama
                        {
                            Id = relatedDrama.SeriesId,
                            Title = relatedDrama.Title,
                            CoverUrl = relatedDrama.CoverUrl,
                        },
                    ]
                    : [],
        };
    }

    private static Post MapFeedPostItem(AppCirclePostItem post)
    {
        AppCirclePostCommon common = post.Common;
        AppCirclePostStat stat = post.Stat;
        AppCircleRelatedDrama? relatedDrama = post.Expand.Videos.FirstOrDefault();
        string id = ValueNormalizer.First(post.PostId, string.Empty);
        string text = ValueNormalizer.First(common.Content.PureText, common.Title, string.Empty);
        List<CircleImage> images = !string.IsNullOrWhiteSpace(common.Cover.Url)
            ?
            [
                new CircleImage
                {
                    Url = common.Cover.Url,
                    Width = common.Cover.Width,
                    Height = common.Cover.Height,
                },
            ]
            : [];

        return new Post
        {
            Id = id,
            Title = ValueNormalizer.First(common.Title, text),
            PureText = text,
            Images = images,
            Author = MapCommentUserInfo(common.UserInfo),
            CreatedAtUnixSeconds = Math.Max(0, common.CreatedAt),
            LikeCount = Math.Max(0, stat.DiggCount),
            CommentCount = Math.Max(0, stat.ReplyCount),
            RelatedDramas =
                relatedDrama is not null && relatedDrama.SeriesId.Length > 0
                    ?
                    [
                        new Drama
                        {
                            Id = relatedDrama.SeriesId,
                            Title = relatedDrama.Title,
                            CoverUrl = relatedDrama.CoverUrl,
                        },
                    ]
                    : [],
        };
    }

    internal static Post MapCirclePost(AppCirclePostItem post)
    {
        AppCirclePostCommon common = post.Common;
        AppCirclePostStat stat = post.Stat;
        AppCirclePostContent content = common.Content;

        List<CircleImage> images = [];
        List<string> topics = [];

        // 尝试解析富文本中的素材列表
        if (!string.IsNullOrWhiteSpace(content.Text))
        {
            try
            {
                byte[] bytes = Encoding.UTF8.GetBytes(content.Text);
                AppCirclePostMaterialContainer? container =
                    JsonAotSerializer.Deserialize<AppCirclePostMaterialContainer>(bytes);

                if (container?.Materials is not null)
                {
                    foreach (AppCirclePostMaterial material in container.Materials)
                    {
                        if (
                            string.Equals(
                                material.Type,
                                "img-list",
                                StringComparison.OrdinalIgnoreCase
                            )
                        )
                        {
                            foreach (AppCirclePostImageItem img in material.Data?.Images ?? [])
                            {
                                if (!string.IsNullOrWhiteSpace(img.WebUrl))
                                {
                                    images.Add(
                                        new CircleImage
                                        {
                                            Url = img.WebUrl,
                                            Width = img.Width,
                                            Height = img.Height,
                                        }
                                    );
                                }
                            }
                        }
                        else if (
                            string.Equals(
                                material.Type,
                                "tag_topic_mention",
                                StringComparison.OrdinalIgnoreCase
                            )
                        )
                        {
                            string tag = material.Data?.TopicTag?.Tag ?? string.Empty;
                            if (tag.Length > 0 && !topics.Contains(tag, StringComparer.Ordinal))
                            {
                                topics.Add(tag.StartsWith('#') ? tag : "#" + tag);
                            }
                        }
                    }
                }
            }
            catch (JsonException)
            {
                // 如果富文本不是 JSON，安全降级
            }
        }

        // 如果素材里没有图片，但有封面，添加封面作为第一张图片
        if (images.Count == 0 && !string.IsNullOrWhiteSpace(common.Cover.Url))
        {
            images.Add(
                new CircleImage
                {
                    Url = common.Cover.Url,
                    Width = common.Cover.Width,
                    Height = common.Cover.Height,
                }
            );
        }

        // 补充从 text_exts 识别的话题
        foreach (AppCirclePostTextExt ext in content.TextExts ?? [])
        {
            if (ext.TextExt == 12 && !string.IsNullOrWhiteSpace(ext.Text))
            {
                string tag = ext.Text.StartsWith('#') ? ext.Text : "#" + ext.Text;
                if (!topics.Contains(tag, StringComparer.Ordinal))
                {
                    topics.Add(tag);
                }
            }
        }

        // 关联短剧
        List<Drama> relatedDramas = [];
        foreach (AppCircleRelatedDrama d in post.Expand.Videos ?? [])
        {
            if (!string.IsNullOrWhiteSpace(d.SeriesId))
            {
                relatedDramas.Add(
                    new Drama
                    {
                        Id = d.SeriesId,
                        Title = d.Title,
                        CoverUrl = d.CoverUrl,
                        Description = d.Introduction,
                        EpisodeCount = Math.Max(0, d.EpisodeCount),
                    }
                );
            }
        }

        string title = ValueNormalizer.First(common.Title, content.PureText, string.Empty);
        return new Post
        {
            Id = post.PostId,
            Title = title,
            PureText = content.PureText,
            Images = images,
            Topics = topics,
            Author = MapCommentUserInfo(common.UserInfo),
            CreatedAtUnixSeconds = Math.Max(0, common.CreatedAt),
            LikeCount = Math.Max(0, stat.DiggCount),
            CommentCount = Math.Max(0, stat.ReplyCount),
            CollectCount = Math.Max(0, stat.CollectCount),
            IsLiked = post.UserAction.UserDigg,
            IsCollected = post.UserAction.UserCollect,
            RelatedDramas = relatedDramas,
        };
    }

    internal static User MapCommentUserInfo(AppCommentUserInfo? info)
    {
        if (info is null)
        {
            return new User();
        }

        AppCommentUserBaseInfo? baseInfo = info.BaseInfo;
        string id = ValueNormalizer.First(info.Id, baseInfo?.UserId, string.Empty);
        string name = ValueNormalizer.First(info.Name, baseInfo?.Name, string.Empty);
        string avatar = ValueNormalizer.First(info.AvatarUrl, baseInfo?.AvatarUrl, string.Empty);
        string intro = ValueNormalizer.First(
            info.Introduction,
            info.Description,
            baseInfo?.Description,
            string.Empty
        );

        return new User
        {
            Id = id,
            Name = name,
            AvatarUrl = avatar,
            Introduction = intro,
        };
    }

    private static long TabTotal(IReadOnlyList<AppUserProfileTab>? tabs, int tabType)
    {
        if (tabs is null)
        {
            return 0;
        }

        foreach (AppUserProfileTab tab in tabs)
        {
            if (tab.Type == tabType)
            {
                return Math.Max(0, tab.Total);
            }
        }

        return 0;
    }

    private static string NormalizeNumericId(string value, string paramName)
    {
        string normalized = (value ?? string.Empty).Trim();
        if (!ValueNormalizer.IsNumericId(normalized))
        {
            throw new ArgumentException("标识 ID 必须为纯数字字符串。", paramName);
        }
        return normalized;
    }

    private static void ValidateCount(int count)
    {
        if (count is < 1 or > MaxPageSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(count),
                $"请求数量必须在 1 到 {MaxPageSize} 之间。"
            );
        }
    }

    private static string NormalizeCursor(string cursor)
    {
        cursor = (cursor ?? string.Empty).Trim();
        if (cursor.Length > 8192 || cursor.Any(static c => c is '\r' or '\n' or '\0'))
        {
            throw new ArgumentException("游标无效。", nameof(cursor));
        }
        return cursor;
    }
}
