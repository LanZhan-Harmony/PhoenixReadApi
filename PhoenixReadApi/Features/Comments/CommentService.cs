using PhoenixReadApi.Internal.Contracts.App;
using PhoenixReadApi.Internal.Contracts.App.Comment;
using PhoenixReadApi.Internal.Features.Shared;
using PhoenixReadApi.Internal.Infrastructure.Http;
using PhoenixReadApi.Internal.Infrastructure.Runtime;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Features.Comments;

/// <summary>加载剧评和分集评论。</summary>
internal sealed class CommentService : PhoenixFeatureService
{
    private const int MaxPageSize = 90;

    internal CommentService(PhoenixReadClientRuntime runtime)
        : base(runtime) { }

    internal Task<CommentPage> GetDramaReviewsAsync(
        string seriesId,
        int count,
        string cursor,
        CancellationToken cancellationToken,
        CommentSortOrder sort = CommentSortOrder.Hot
    )
    {
        string normalizedSeriesId = NormalizeSeriesId(seriesId);
        return LoadAsync(
            normalizedSeriesId,
            normalizedSeriesId,
            isEpisode: false,
            count,
            cursor,
            cancellationToken,
            sort
        );
    }

    internal Task<CommentPage> GetEpisodeCommentsAsync(
        string seriesId,
        string videoId,
        int count,
        string cursor,
        CancellationToken cancellationToken,
        CommentSortOrder sort = CommentSortOrder.Hot
    )
    {
        string normalizedSeriesId = NormalizeSeriesId(seriesId);
        string normalizedVideoId = NormalizeVideoId(videoId);
        return LoadAsync(
            normalizedSeriesId,
            normalizedVideoId,
            isEpisode: true,
            count,
            cursor,
            cancellationToken,
            sort
        );
    }

    internal Task<CommentReplyPage> GetDramaReviewRepliesAsync(
        string commentId,
        string seriesId,
        int count,
        string cursor,
        CancellationToken cancellationToken
    ) => GetRepliesAsync(commentId, seriesId, string.Empty, count, cursor, cancellationToken);

    internal Task<CommentReplyPage> GetEpisodeCommentRepliesAsync(
        string commentId,
        string seriesId,
        string videoId,
        int count,
        string cursor,
        CancellationToken cancellationToken
    ) => GetRepliesAsync(commentId, seriesId, videoId, count, cursor, cancellationToken);

    internal async Task<CommentReplyPage> GetRepliesAsync(
        string commentId,
        string seriesId,
        string videoId,
        int count,
        string cursor,
        CancellationToken cancellationToken
    )
    {
        commentId = NormalizeCommentId(commentId);
        cursor = cursor?.Trim() ?? string.Empty;
        ValidateReplyPage(count, cursor);
        AppReplyListData data = await LoadRepliesAsync(
                commentId,
                seriesId,
                videoId,
                count,
                cursor,
                cancellationToken
            )
            .ConfigureAwait(false);
        IReadOnlyList<AppReplyRecord> replyItems = data.ReplyItems is { Count: > 0 }
            ? data.ReplyItems
            : data.Items ?? [];
        AppReplyListInfo info = data.CommentListInfo is { Total: > 0 }
            ? data.CommentListInfo
            : data.ListInfo ?? new();
        return new CommentReplyPage
        {
            Items = [.. replyItems.Select(MapReply).Where(static item => item.Id.Length > 0)],
            Total = Math.Max(info.Total, replyItems.Count),
            Cursor = info.Cursor ?? string.Empty,
            HasMore = info.HasMore,
        };
    }

    private async Task<AppReplyListData> LoadRepliesAsync(
        string commentId,
        string seriesId,
        string videoId,
        int count,
        string cursor,
        CancellationToken cancellationToken
    )
    {
        bool isEpisode = !string.IsNullOrWhiteSpace(videoId) && !string.Equals(seriesId, videoId, StringComparison.Ordinal);
        string normalizedSeriesId = string.IsNullOrWhiteSpace(seriesId)
            ? string.Empty
            : NormalizeSeriesOrVideoId(seriesId);
        string normalizedVideoId = isEpisode
            ? NormalizeSeriesOrVideoId(videoId)
            : string.Empty;

        int commentSource = isEpisode ? 504 : 501;
        int serverChannel = isEpisode ? 18 : 34;
        int commentType = isEpisode ? 4 : 2;
        int groupType = isEpisode ? 30 : 1;
        string groupId = isEpisode ? normalizedVideoId : normalizedSeriesId;
        string path = isEpisode
            ? "/novel/commentapi/reply/list/" + commentId + "/v1/"
            : "/novel/commentapi/reply/list/" + commentId + "/v1";

        AppReplyListPayload payload = new()
        {
            CommentSource = commentSource,
            ServerChannel = serverChannel,
            CommentId = commentId,
            CommentType = commentType,
            GroupId = groupId,
            GroupType = groupType,
            Count = count,
            Cursor = cursor,
            Aid = 8662,
            ComplianceStatus = 0,
            BusinessParameter = new AppReplyBusinessParameter
            {
                BookId = normalizedSeriesId.Length > 0 ? normalizedSeriesId : groupId,
                NeedCount = cursor.Length == 0,
                RealLevel = isEpisode ? 0 : 2,
            },
        };

        AppResponse<AppReplyListData> response = await _appTransport
            .PostAsync<AppReplyListPayload, AppReplyListData>(
                path,
                payload,
                commentRequest: true,
                cancellationToken,
                commentSource: commentSource,
                serverChannel: serverChannel
            )
            .ConfigureAwait(false);
        return response.Data ?? new();
    }

    internal Task<PhoenixOperationResult> SendCommentAsync(
        string seriesId,
        string videoId,
        string text,
        CancellationToken cancellationToken
    ) => AddCommentAsync(seriesId, videoId, text, isEpisode: true, score: 0, cancellationToken);

    internal Task<PhoenixOperationResult> SendReviewAsync(
        string seriesId,
        double rating,
        string text,
        CancellationToken cancellationToken
    )
    {
        if (rating is < 0.5 or > 5 || Math.Abs(rating * 2 - Math.Round(rating * 2)) > 0.0001)
        {
            throw new ArgumentOutOfRangeException(
                nameof(rating),
                "剧评分必须是 0.5 到 5 之间的半星倍数。"
            );
        }
        return AddCommentAsync(
            seriesId,
            seriesId,
            text,
            isEpisode: false,
            ScoreFromRating(rating),
            cancellationToken
        );
    }

    internal Task<PhoenixOperationResult> SendReviewAsync(
        string seriesId,
        string text,
        CancellationToken cancellationToken
    ) => SendReviewAsync(seriesId, 5.0, text, cancellationToken);

    internal Task<PhoenixOperationResult> SendDanmakuAsync(
        string seriesId,
        string videoId,
        string text,
        CancellationToken cancellationToken
    ) =>
        AddCommentAsync(
            seriesId,
            videoId,
            text,
            isEpisode: true,
            score: 0,
            cancellationToken,
            danmaku: true
        );

    internal async Task<PhoenixOperationResult> LikeCommentAsync(
        string commentId,
        string groupId,
        bool active,
        CancellationToken cancellationToken,
        int objectType = 2,
        int commentType = 4
    )
    {
        commentId = NormalizeCommentId(commentId);
        groupId = NormalizeSeriesOrVideoId(groupId);
        AppResponse<AppOperationResponse> response = await _appTransport
            .PostAsync<AppCommentActionPayload, AppOperationResponse>(
                "/novel/commentapi/comment/do_action/v1/",
                new AppCommentActionPayload
                {
                    ObjectId = commentId,
                    ObjectType = objectType,
                    ActionType = active ? 8 : 9,
                    CommentType = commentType,
                    BusinessParameter = new AppCommentActionBusinessParameter { BookId = groupId },
                },
                commentRequest: true,
                cancellationToken,
                retryMode: AppRequestRetryMode.NoRetry
            )
            .ConfigureAwait(false);
        return Operation(response.Message);
    }

    internal async Task<PhoenixOperationResult> DeleteCommentAsync(
        string commentId,
        string groupId,
        CancellationToken cancellationToken,
        int dataType = 4,
        int serviceId = 4,
        int groupType = 30
    )
    {
        commentId = NormalizeCommentId(commentId);
        groupId = NormalizeSeriesOrVideoId(groupId);
        bool isReview = groupType == 1;
        int delCommentSource = isReview ? 501 : 504;
        int delServerChannel = isReview ? 34 : 18;

        AppResponse<AppOperationResponse> response = await _appTransport
            .PostAsync<AppCommentDeletePayload, AppOperationResponse>(
                "/novel/commentapi/comment/del/v1/",
                new AppCommentDeletePayload
                {
                    CommentId = commentId,
                    GroupId = groupId,
                    GroupType = groupType,
                    DataType = dataType,
                    ServiceId = serviceId,
                },
                commentRequest: true,
                cancellationToken,
                commentSource: delCommentSource,
                serverChannel: delServerChannel,
                retryMode: AppRequestRetryMode.NoRetry
            )
            .ConfigureAwait(false);
        return Operation(response.Message, commentId);
    }

    internal async Task<PhoenixOperationResult> SendReplyAsync(
        string commentId,
        string groupId,
        string text,
        string replyToUserId,
        CancellationToken cancellationToken
    )
    {
        commentId = NormalizeCommentId(commentId);
        groupId = NormalizeSeriesOrVideoId(groupId);
        text = NormalizeText(text);
        AppResponse<AppReplyAddData> response = await _appTransport
            .PostAsync<AppCommentAddPayload, AppReplyAddData>(
                "/novel/commentapi/reply/add/v1/",
                new AppCommentAddPayload
                {
                    CommitSource = 9,
                    Text = text,
                    GroupId = groupId,
                    GroupType = 30,
                    DataType = 4,
                    ReplyToCommentId = commentId,
                    ReplyToUserId = replyToUserId,
                    BusinessParameter = new AppCommentAddBusinessParameter { BookId = groupId },
                },
                commentRequest: true,
                cancellationToken,
                retryMode: AppRequestRetryMode.NoRetry
            )
            .ConfigureAwait(false);
        string replyId = ValueNormalizer.First(
            response.Data?.Reply?.ReplyId,
            response.Data?.Reply?.Id,
            response.Data?.ReplyId
        );
        return Operation(response.Message, replyId);
    }

    internal Task<PhoenixOperationResult> SendReplyAsync(
        string commentId,
        string text,
        string replyToUserId,
        CancellationToken cancellationToken
    ) => SendReplyAsync(commentId, commentId, text, replyToUserId, cancellationToken);

    internal Task<PhoenixOperationResult> DeleteReplyAsync(
        string replyId,
        CancellationToken cancellationToken
    ) => DeleteCommentAsync(replyId, replyId, cancellationToken, dataType: 9, serviceId: 4, groupType: 0);

    private async Task<CommentPage> LoadAsync(
        string seriesId,
        string targetId,
        bool isEpisode,
        int count,
        string cursor,
        CancellationToken cancellationToken,
        CommentSortOrder sort = CommentSortOrder.Hot
    )
    {
        if (count is < 1 or > MaxPageSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(count),
                $"评论数量必须在 1 到 {MaxPageSize} 之间。"
            );
        }

        cursor = (cursor ?? string.Empty).Trim();
        if (cursor.Length > 8192 || cursor.Any(static c => c is '\r' or '\n' or '\0'))
        {
            throw new ArgumentException("评论游标无效。", nameof(cursor));
        }

        AppCommentListPayload payload = isEpisode
            ? new()
            {
                CommentSource = 4,
                ServerChannel = 18,
                GroupId = targetId,
                GroupType = 30,
                CommentType = 4,
                Count = count,
                Cursor = cursor,
                Sort = (int)sort,
                BusinessParameter = new AppCommentListBusinessParameter
                {
                    BookId = seriesId,
                    NeedCount = cursor.Length == 0,
                },
            }
            : new()
            {
                CommentSource = 1,
                ServerChannel = 34,
                GroupId = targetId,
                GroupType = 1,
                CommentType = 2,
                Count = count,
                Cursor = cursor,
                Sort = (int)sort,
                BusinessParameter = new AppCommentListBusinessParameter
                {
                    BookId = seriesId,
                    NeedCount = cursor.Length == 0,
                },
            };

        AppResponse<AppCommentListData> response = await _appTransport
            .PostAsync<AppCommentListPayload, AppCommentListData>(
                "/novel/commentapi/comment/list/" + targetId + "/v1/",
                payload,
                commentRequest: true,
                cancellationToken,
                commentSource: payload.CommentSource,
                serverChannel: payload.ServerChannel
            )
            .ConfigureAwait(false);
        AppCommentListData data =
            response.Data
            ?? throw new PhoenixReadApiResponseException("评论接口未返回有效数据。", "comments");

        List<DramaComment> items = [];
        HashSet<string> seen = new(StringComparer.Ordinal);
        foreach (AppCommentRecord? record in data.Items ?? [])
        {
            if (
                record is null
                || record.Comment is null
                || record.Comment.Common is null
                || record.Comment.Common.Content is null
            )
            {
                continue;
            }

            AppComment comment = record.Comment;
            string groupId = comment.Common.GroupId ?? string.Empty;
            if (comment.Common.Status != "1" || (groupId.Length > 0 && groupId != targetId))
            {
                continue;
            }

            string id = (comment.Id ?? string.Empty).Trim();
            string text = SanitizeText(comment.Common.Content?.Text ?? string.Empty);
            if (id.Length == 0 || id.Length > 120 || text.Length == 0 || !seen.Add(id))
            {
                continue;
            }

            AppCommentUserInfo userInfo = comment.Common.UserInfo ?? new();
            AppCommentExpand expand = comment.Expand ?? new();
            AppCommentStat stat = comment.Stat ?? new();
            string userName = userInfo.Name ?? string.Empty;
            string userAvatar = userInfo.AvatarUrl ?? string.Empty;
            AppCommentUserBaseInfo? baseInfo = userInfo.BaseInfo;
            items.Add(
                new DramaComment
                {
                    Id = id,
                    Text = text.Length > 4000 ? string.Concat(text.AsSpan(0, 4000), "…") : text,
                    Author = new User
                    {
                        Id = ValueNormalizer.First(userInfo.Id, baseInfo?.UserId, string.Empty),
                        Name = userName.Length > 0 ? userName : baseInfo?.Name ?? string.Empty,
                        AvatarUrl =
                            userAvatar.Length > 0
                                ? userAvatar
                                : baseInfo?.AvatarUrl ?? string.Empty,
                    },
                    CreatedAtUnixSeconds = comment.Common.CreatedAt,
                    OffsetMs = isEpisode ? Math.Max(0, expand.OffsetTime) : 0,
                    LikeCount = Math.Max(0, stat.LikeCount),
                    ReplyCount = Math.Max(0, stat.ReplyCount),
                }
            );
        }

        AppCommentListInfo listInfo = data.ListInfo ?? new();
        return new CommentPage
        {
            TargetId = targetId,
            Items = items,
            Cursor = listInfo.Cursor ?? string.Empty,
            HasMore = listInfo.HasMore,
            Total = Math.Max(listInfo.Total, items.Count),
        };
    }

    private static string SanitizeText(string value) =>
        new string([
            .. value.Select(static c => char.IsControl(c) || c is '\u2028' or '\u2029' ? ' ' : c),
        ]).Trim();

    private async Task<PhoenixOperationResult> AddCommentAsync(
        string seriesId,
        string targetId,
        string text,
        bool isEpisode,
        int score,
        CancellationToken cancellationToken,
        bool danmaku = false
    )
    {
        seriesId = NormalizeSeriesId(seriesId);
        targetId = isEpisode ? NormalizeVideoId(targetId) : seriesId;
        text = NormalizeText(text);
        int reqCommentSource = isEpisode ? 504 : 501;
        int reqServerChannel = isEpisode ? 18 : 34;
        int commentType = danmaku ? 20 : isEpisode ? 4 : 2;

        AppResponse<AppCommentAddData> response = await _appTransport
            .PostAsync<AppCommentAddPayload, AppCommentAddData>(
                "/novel/commentapi/comment/add/v1/",
                new AppCommentAddPayload
                {
                    CommitSource = isEpisode ? (danmaku ? 1500 : 3) : 12,
                    Text = text,
                    GroupId = targetId,
                    GroupType = isEpisode ? 30 : 1,
                    DataType =
                        danmaku ? 20
                        : isEpisode ? 4
                        : 2,
                    CommentType = commentType,
                    Score = score,
                    BusinessParameter = new AppCommentAddBusinessParameter
                    {
                        BookId = seriesId,
                        Offset = 0,
                        Score = score,
                        VideoId = isEpisode ? targetId : string.Empty,
                    },
                },
                commentRequest: true,
                cancellationToken,
                commentSource: reqCommentSource,
                serverChannel: reqServerChannel,
                retryMode: AppRequestRetryMode.NoRetry
            )
            .ConfigureAwait(false);
        string createdId = ValueNormalizer.First(
            response.Data?.CommentInfo?.CommentId,
            response.Data?.CommentId
        );
        return Operation(response.Message, createdId);
    }

    private static CommentReply MapReply(AppReplyRecord record)
    {
        string id = ValueNormalizer.First(record.ReplyId, record.Reply?.Id);
        string commentId = ValueNormalizer.First(record.ReplyToCommentId, record.Reply?.CommentId);
        string replyToUserId = ValueNormalizer.First(
            record.ReplyToUserInfo?.BaseInfo?.UserId,
            record.Reply?.ReplyToUserId
        );
        string text = SanitizeText(
            ValueNormalizer.First(record.Common?.Content?.Text, record.Reply?.Content?.Text)
        );
        string userId = ValueNormalizer.First(
            record.Common?.UserInfo?.BaseInfo?.UserId,
            record.Reply?.User?.Id
        );
        string userName = ValueNormalizer.First(
            record.Common?.UserInfo?.BaseInfo?.UserName,
            record.Reply?.User?.Name
        );
        string avatarUrl = ValueNormalizer.First(
            record.Common?.UserInfo?.BaseInfo?.UserAvatar,
            record.Reply?.User?.AvatarUrl
        );
        long createdAt = record.Common?.CreateTimestamp > 0
            ? record.Common.CreateTimestamp
            : record.Reply?.CreatedAt ?? 0;
        long likeCount = record.Stat?.DiggCount > 0
            ? record.Stat.DiggCount
            : record.Reply?.LikeCount ?? 0;

        return new CommentReply
        {
            Id = id,
            CommentId = commentId,
            ReplyToUserId = replyToUserId,
            Text = text,
            Author = new User
            {
                Id = userId,
                Name = userName,
                AvatarUrl = avatarUrl,
            },
            CreatedAtUnixSeconds = Math.Max(0, createdAt),
            LikeCount = Math.Max(0, likeCount),
        };
    }

    private static int ScoreFromRating(double rating) =>
        (int)Math.Round(rating * 2 - 1, MidpointRounding.AwayFromZero);

    private static void ValidateReplyPage(int count, string cursor)
    {
        if (count is < 1 or > MaxPageSize)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }
        if (cursor.Length > 8192 || cursor.Any(static c => c is '\r' or '\n' or '\0'))
        {
            throw new ArgumentException("评论回复游标无效。", nameof(cursor));
        }
    }

    private static string NormalizeCommentId(string value)
    {
        value = (value ?? string.Empty).Trim();
        if (value.Length == 0 || value.Length > 128)
        {
            throw new ArgumentException("评论 ID 无效。", nameof(value));
        }
        return value;
    }

    private static string NormalizeSeriesOrVideoId(string value)
    {
        value = (value ?? string.Empty).Trim();
        return ValueNormalizer.IsNumericId(value)
            ? value
            : throw new ArgumentException("剧集或视频 ID 无效。", nameof(value));
    }

    private static string NormalizeText(string value)
    {
        value = SanitizeText(value ?? string.Empty);
        if (value.Length == 0 || value.Length > 4000)
        {
            throw new ArgumentException("内容必须为 1 到 4000 个字符。", nameof(value));
        }
        return value;
    }

    private static PhoenixOperationResult Operation(string message, string id = "") =>
        new()
        {
            Succeeded = true,
            Message = message ?? string.Empty,
            Id = id ?? string.Empty,
        };
}
