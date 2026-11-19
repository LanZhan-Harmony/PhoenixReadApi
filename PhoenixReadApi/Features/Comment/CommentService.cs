using PhoenixReadApi.Internal.Contracts.App;
using PhoenixReadApi.Internal.Contracts.App.Comment;
using PhoenixReadApi.Internal.Features.Shared;
using PhoenixReadApi.Internal.Infrastructure.Http;
using PhoenixReadApi.Internal.Infrastructure.Runtime;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Features.Comment;

using Comment = PhoenixReadApi.Comment;
using User = PhoenixReadApi.User;

/// <summary>加载剧评和分集评论。</summary>
internal sealed class CommentService : PhoenixFeatureService
{
    private const int MaxPageSize = 90;

    internal CommentService(PhoenixReadClientRuntime runtime)
        : base(runtime) { }

    internal Task<DramaReviewPage> GetDramaReviewsAsync(
        string dramaId,
        int count,
        string cursor,
        CancellationToken cancellationToken,
        CommentSortOrder sort = CommentSortOrder.Hot
    )
    {
        string normalizedDramaId = NormalizeDramaId(dramaId);
        return LoadDramaReviewsInternalAsync(
            dramaId: normalizedDramaId,
            count: count,
            cursor: cursor,
            cancellationToken: cancellationToken,
            sort: sort
        );
    }

    internal Task<CommentPage> GetEpisodeCommentsAsync(
        string dramaId,
        string videoId,
        int count,
        string cursor,
        CancellationToken cancellationToken,
        CommentSortOrder sort = CommentSortOrder.Hot
    )
    {
        string normalizedDramaId = NormalizeDramaId(dramaId);
        string normalizedVideoId = NormalizeVideoId(videoId);
        return LoadAsync(
            normalizedDramaId,
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
        string dramaId,
        int count,
        string cursor,
        CancellationToken cancellationToken
    ) => GetRepliesAsync(commentId, dramaId, string.Empty, count, cursor, cancellationToken);

    internal Task<CommentReplyPage> GetEpisodeCommentRepliesAsync(
        string commentId,
        string dramaId,
        string videoId,
        int count,
        string cursor,
        CancellationToken cancellationToken
    ) => GetRepliesAsync(commentId, dramaId, videoId, count, cursor, cancellationToken);

    internal async Task<CommentReplyPage> GetRepliesAsync(
        string commentId,
        string dramaId,
        string videoId,
        int count,
        string cursor,
        CancellationToken cancellationToken
    )
    {
        commentId = NormalizeCommentId(commentId);
        cursor = cursor?.Trim() ?? string.Empty;
        ValidateReplyPage(count, cursor);

        bool isEpisode =
            !string.IsNullOrWhiteSpace(videoId)
            && !string.Equals(dramaId, videoId, StringComparison.Ordinal);
        string normalizedDramaId = string.IsNullOrWhiteSpace(dramaId)
            ? string.Empty
            : NormalizeDramaOrVideoId(dramaId);
        string normalizedVideoId = isEpisode ? NormalizeDramaOrVideoId(videoId) : string.Empty;

        return await LoadRepliesInternalAsync(
                commentId: commentId,
                groupId: isEpisode ? normalizedVideoId : normalizedDramaId,
                bookId: normalizedDramaId.Length > 0 ? normalizedDramaId : (isEpisode ? normalizedVideoId : string.Empty),
                commentSource: isEpisode ? 504 : 501,
                serverChannel: isEpisode ? 18 : 34,
                groupType: isEpisode ? 30 : 1,
                commentType: isEpisode ? 4 : 2,
                realLevel: isEpisode ? 0 : 2,
                isTrailingSlash: isEpisode,
                count: count,
                cursor: cursor,
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(false);
    }

    internal async Task<CommentReplyPage> GetCirclePostCommentRepliesAsync(
        string commentId,
        string postId,
        int count,
        string cursor,
        CancellationToken cancellationToken
    )
    {
        commentId = NormalizeCommentId(commentId);
        postId = NormalizeCommentId(postId);
        cursor = cursor?.Trim() ?? string.Empty;
        ValidateReplyPage(count, cursor);

        return await LoadRepliesInternalAsync(
                commentId: commentId,
                groupId: postId,
                bookId: postId,
                commentSource: 5,
                serverChannel: 32,
                groupType: 3,
                commentType: 11,
                realLevel: 2,
                isTrailingSlash: false,
                count: count,
                cursor: cursor,
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(false);
    }

    internal async Task<CommentReplyPage> GetCircleVideoCommentRepliesAsync(
        string commentId,
        string videoId,
        int count,
        string cursor,
        CancellationToken cancellationToken
    )
    {
        commentId = NormalizeCommentId(commentId);
        videoId = NormalizeDramaOrVideoId(videoId);
        cursor = cursor?.Trim() ?? string.Empty;
        ValidateReplyPage(count, cursor);

        return await LoadRepliesInternalAsync(
                commentId: commentId,
                groupId: videoId,
                bookId: videoId,
                commentSource: 4,
                serverChannel: 26,
                groupType: 1,
                commentType: 4,
                realLevel: 0,
                isTrailingSlash: true,
                count: count,
                cursor: cursor,
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(false);
    }

    private async Task<CommentReplyPage> LoadRepliesInternalAsync(
        string commentId,
        string groupId,
        string bookId,
        int commentSource,
        int serverChannel,
        int groupType,
        int commentType,
        int realLevel,
        bool isTrailingSlash,
        int count,
        string cursor,
        CancellationToken cancellationToken
    )
    {
        string path = isTrailingSlash
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
                BookId = bookId,
                NeedCount = cursor.Length == 0,
                RealLevel = realLevel,
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

        AppReplyListData data = response.Data ?? new();
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

    internal Task<PhoenixOperationResult> SendCommentAsync(
        string dramaId,
        string videoId,
        string text,
        CancellationToken cancellationToken
    ) => AddCommentAsync(dramaId, videoId, text, isEpisode: true, score: 0, cancellationToken);

    internal Task<PhoenixOperationResult> SendReviewAsync(
        string dramaId,
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
            dramaId,
            dramaId,
            text,
            isEpisode: false,
            ScoreFromRating(rating),
            cancellationToken
        );
    }

    internal Task<PhoenixOperationResult> SendReviewAsync(
        string dramaId,
        string text,
        CancellationToken cancellationToken
    ) => SendReviewAsync(dramaId, 5.0, text, cancellationToken);

    internal Task<PhoenixOperationResult> SendDanmakuAsync(
        string dramaId,
        string videoId,
        string text,
        CancellationToken cancellationToken
    ) =>
        AddCommentAsync(
            dramaId,
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
        int commentType = 4,
        int? commentSource = null,
        int? serverChannel = null
    )
    {
        commentId = NormalizeCommentId(commentId);
        groupId = NormalizeDramaOrVideoId(groupId);
        bool isReview = commentType == 2;
        int likeCommentSource = commentSource ?? (isReview ? 501 : 504);
        int likeServerChannel = serverChannel ?? (isReview ? 34 : 18);

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
                commentSource: likeCommentSource,
                serverChannel: likeServerChannel,
                retryMode: AppRequestRetryMode.NoRetry
            )
            .ConfigureAwait(false);
        return Operation(response.Message);
    }

    internal Task<PhoenixOperationResult> LikeCirclePostCommentAsync(
        string commentId,
        string postId,
        bool active,
        CancellationToken cancellationToken
    ) =>
        LikeCommentAsync(
            commentId,
            postId,
            active,
            cancellationToken,
            objectType: 2,
            commentType: 11,
            commentSource: 5,
            serverChannel: 32
        );

    internal Task<PhoenixOperationResult> LikeCirclePostReplyAsync(
        string replyId,
        string postId,
        bool active,
        CancellationToken cancellationToken
    ) =>
        LikeCommentAsync(
            replyId,
            postId,
            active,
            cancellationToken,
            objectType: 8,
            commentType: 11,
            commentSource: 5,
            serverChannel: 32
        );

    internal Task<PhoenixOperationResult> LikeCircleVideoCommentAsync(
        string commentId,
        string videoId,
        bool active,
        CancellationToken cancellationToken
    ) =>
        LikeCommentAsync(
            commentId,
            videoId,
            active,
            cancellationToken,
            objectType: 2,
            commentType: 4,
            commentSource: 4,
            serverChannel: 26
        );

    internal Task<PhoenixOperationResult> LikeCircleVideoReplyAsync(
        string replyId,
        string videoId,
        bool active,
        CancellationToken cancellationToken
    ) =>
        LikeCommentAsync(
            replyId,
            videoId,
            active,
            cancellationToken,
            objectType: 8,
            commentType: 4,
            commentSource: 4,
            serverChannel: 26
        );

    internal async Task<PhoenixOperationResult> DeleteCommentAsync(
        string commentId,
        string groupId,
        CancellationToken cancellationToken,
        int dataType = 4,
        int serviceId = 4,
        int groupType = 30,
        int? commentSource = null,
        int? serverChannel = null
    )
    {
        commentId = NormalizeCommentId(commentId);
        groupId = NormalizeDramaOrVideoId(groupId);
        bool isReview = groupType == 1;
        int delCommentSource = commentSource ?? (isReview ? 501 : 504);
        int delServerChannel = serverChannel ?? (isReview ? 34 : 18);

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

    internal Task<PhoenixOperationResult> DeleteCirclePostCommentAsync(
        string commentId,
        string postId,
        CancellationToken cancellationToken
    ) =>
        DeleteCommentAsync(
            commentId,
            postId,
            cancellationToken,
            dataType: 4,
            serviceId: 4,
            groupType: 3,
            commentSource: 5,
            serverChannel: 32
        );

    internal Task<PhoenixOperationResult> DeleteCircleVideoCommentAsync(
        string commentId,
        string videoId,
        CancellationToken cancellationToken
    ) =>
        DeleteCommentAsync(
            commentId,
            videoId,
            cancellationToken,
            dataType: 4,
            serviceId: 4,
            groupType: 1,
            commentSource: 4,
            serverChannel: 26
        );

    internal async Task<PhoenixOperationResult> SendReplyAsync(
        string commentId,
        string dramaId,
        string videoId,
        string text,
        string? replyToUserId,
        CancellationToken cancellationToken
    )
    {
        commentId = NormalizeCommentId(commentId);
        dramaId = NormalizeDramaId(dramaId);
        text = NormalizeText(text);

        bool isEpisode =
            !string.IsNullOrWhiteSpace(videoId)
            && !string.Equals(dramaId, videoId, StringComparison.Ordinal);
        string normalizedVideoId = isEpisode ? NormalizeVideoId(videoId) : string.Empty;

        string groupId = isEpisode ? normalizedVideoId : dramaId;
        int commentSource = isEpisode ? 504 : 501;
        int serverChannel = isEpisode ? 18 : 34;
        int groupType = isEpisode ? 30 : 1;
        int dataType = isEpisode ? 4 : 2;
        int commentType = isEpisode ? 4 : 2;

        string? normalizedReplyUser = string.IsNullOrWhiteSpace(replyToUserId)
            ? null
            : replyToUserId.Trim();

        AppResponse<AppReplyAddData> response = await _appTransport
            .PostAsync<AppCommentAddPayload, AppReplyAddData>(
                "/novel/commentapi/reply/add/v1/",
                new AppCommentAddPayload
                {
                    CommitSource = 9,
                    Text = text,
                    GroupId = groupId,
                    GroupType = groupType,
                    DataType = dataType,
                    CommentType = commentType,
                    ReplyToCommentId = commentId,
                    ReplyToUserId = normalizedReplyUser,
                    BusinessParameter = new AppCommentAddBusinessParameter
                    {
                        BookId = dramaId,
                        VideoId = isEpisode ? normalizedVideoId : null,
                    },
                },
                commentRequest: true,
                cancellationToken,
                commentSource: commentSource,
                serverChannel: serverChannel,
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

    internal Task<PhoenixOperationResult> SendDramaReviewReplyAsync(
        string commentId,
        string dramaId,
        string text,
        string? replyToUserId,
        CancellationToken cancellationToken
    ) =>
        SendReplyAsync(
            commentId,
            dramaId,
            videoId: string.Empty,
            text,
            replyToUserId,
            cancellationToken
        );

    internal Task<PhoenixOperationResult> DeleteReplyAsync(
        string replyId,
        CancellationToken cancellationToken
    ) =>
        DeleteCommentAsync(
            replyId,
            replyId,
            cancellationToken,
            dataType: 9,
            serviceId: 4,
            groupType: 0
        );

    internal Task<CommentPage> GetCirclePostCommentsAsync(
        string postId,
        int count,
        string cursor,
        CancellationToken cancellationToken,
        CommentSortOrder sort = CommentSortOrder.Hot
    )
    {
        postId = NormalizeCommentId(postId);
        return LoadCommentsInternalAsync(
            bookId: postId,
            targetId: postId,
            commentSource: 5,
            serverChannel: 32,
            groupType: 3,
            commentType: 11,
            count: count,
            cursor: cursor,
            cancellationToken: cancellationToken,
            sort: sort,
            isEpisode: false
        );
    }

    internal Task<CommentPage> GetCircleVideoCommentsAsync(
        string videoId,
        int count,
        string cursor,
        CancellationToken cancellationToken,
        CommentSortOrder sort = CommentSortOrder.Hot
    )
    {
        videoId = NormalizeDramaOrVideoId(videoId);
        return LoadCommentsInternalAsync(
            bookId: videoId,
            targetId: videoId,
            commentSource: 4,
            serverChannel: 26,
            groupType: 1,
            commentType: 4,
            count: count,
            cursor: cursor,
            cancellationToken: cancellationToken,
            sort: sort,
            isEpisode: false
        );
    }

    private async Task<DramaReviewPage> LoadDramaReviewsInternalAsync(
        string dramaId,
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

        AppCommentListPayload payload = new()
        {
            CommentSource = 1,
            ServerChannel = 34,
            GroupId = dramaId,
            GroupType = 1,
            CommentType = 2,
            Count = count,
            Cursor = cursor,
            Sort = (int)sort,
            BusinessParameter = new AppCommentListBusinessParameter
            {
                BookId = dramaId,
                NeedCount = cursor.Length == 0,
            },
        };

        AppResponse<AppCommentListData> response = await _appTransport
            .PostAsync<AppCommentListPayload, AppCommentListData>(
                "/novel/commentapi/comment/list/" + dramaId + "/v1/",
                payload,
                commentRequest: true,
                cancellationToken,
                commentSource: payload.CommentSource,
                serverChannel: payload.ServerChannel
            )
            .ConfigureAwait(false);
        AppCommentListData data = response.Data
            ?? throw new PhoenixReadApiResponseException("剧评接口未返回有效数据。", "drama-reviews");

        List<DramaReview> items = [];
        HashSet<string> seen = [with(StringComparer.Ordinal)];
        foreach (AppCommentRecord? record in data.Items ?? [])
        {
            if (record?.Comment?.Common?.Content is null)
            {
                continue;
            }

            AppComment comment = record.Comment;
            string groupId = comment.Common.GroupId ?? string.Empty;
            if (comment.Common.Status != "1" || (groupId.Length > 0 && groupId != dramaId))
            {
                continue;
            }

            string id = (comment.Id ?? string.Empty).Trim();
            string text = SanitizeText(comment.Common.Content?.Text ?? string.Empty);
            IReadOnlyList<CommentImage> images = MapCommentImages(comment);
            if (
                id.Length == 0
                || id.Length > 120
                || (text.Length == 0 && images.Count == 0)
                || !seen.Add(id)
            )
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
                new DramaReview
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
                    Rating = expand.Score,
                    RatingText = expand.ScoreSuffixText,
                    LikeCount = Math.Max(0, stat.LikeCount),
                    ReplyCount = Math.Max(0, stat.ReplyCount),
                    Images = images,
                }
            );
        }

        if (sort == CommentSortOrder.Latest && items.Count > 1)
        {
            items.Sort(static (a, b) => b.CreatedAtUnixSeconds.CompareTo(a.CreatedAtUnixSeconds));
        }

        AppCommentListInfo listInfo = data.ListInfo ?? new();
        AppCommentListExtra extra = data.Extra ?? new();
        DramaReviewAiSummary? aiSummary = MapAiSummary(extra.AiSummary);

        return new DramaReviewPage
        {
            DramaId = dramaId,
            Items = items,
            DramaScore = extra.CredibilityScore > 0 ? extra.CredibilityScore : null,
            ScoreCount = Math.Max(0, extra.CredibilityScoreCount),
            AiSummary = aiSummary,
            Cursor = listInfo.Cursor ?? string.Empty,
            HasMore = listInfo.HasMore,
            Total = Math.Max(listInfo.Total, items.Count),
        };
    }

    private static DramaReviewAiSummary? MapAiSummary(AppCommentListAiSummary? aiSummary)
    {
        if (
            aiSummary is null
            || (
                string.IsNullOrWhiteSpace(aiSummary.OverallSummary)
                && (aiSummary.Items is null || aiSummary.Items.Count == 0)
            )
        )
        {
            return null;
        }

        return new DramaReviewAiSummary
        {
            SummaryId = aiSummary.SummaryId,
            OverallSummary = aiSummary.OverallSummary,
            Items = [
                .. (aiSummary.Items ?? [])
                    .Where(static item => item is not null)
                    .Select(static item => new DramaReviewAiSummaryItem
                    {
                        Dimension = item.Dimension,
                        Summary = item.SubComment,
                    }),
            ],
        };
    }

    private Task<CommentPage> LoadAsync(
        string dramaId,
        string targetId,
        bool isEpisode,
        int count,
        string cursor,
        CancellationToken cancellationToken,
        CommentSortOrder sort = CommentSortOrder.Hot
    ) =>
        LoadCommentsInternalAsync(
            bookId: dramaId,
            targetId: targetId,
            commentSource: isEpisode ? 4 : 1,
            serverChannel: isEpisode ? 18 : 34,
            groupType: isEpisode ? 30 : 1,
            commentType: isEpisode ? 4 : 2,
            count: count,
            cursor: cursor,
            cancellationToken: cancellationToken,
            sort: sort,
            isEpisode: isEpisode
        );

    private async Task<CommentPage> LoadCommentsInternalAsync(
        string bookId,
        string targetId,
        int commentSource,
        int serverChannel,
        int groupType,
        int commentType,
        int count,
        string cursor,
        CancellationToken cancellationToken,
        CommentSortOrder sort = CommentSortOrder.Hot,
        bool isEpisode = false
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

        AppCommentListPayload payload = new()
        {
            CommentSource = commentSource,
            ServerChannel = serverChannel,
            GroupId = targetId,
            GroupType = groupType,
            CommentType = commentType,
            Count = count,
            Cursor = cursor,
            Sort = (int)sort,
            BusinessParameter = new AppCommentListBusinessParameter
            {
                BookId = bookId,
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
        AppCommentListData data = response.Data
            ?? throw new PhoenixReadApiResponseException("评论接口未返回有效数据。", "comments");

        List<Comment> items = [];
        HashSet<string> seen = [with(StringComparer.Ordinal)];
        foreach (AppCommentRecord? record in data.Items ?? [])
        {
            if (record?.Comment?.Common?.Content is null)
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
            IReadOnlyList<CommentImage> images = MapCommentImages(comment);
            if (
                id.Length == 0
                || id.Length > 120
                || (text.Length == 0 && images.Count == 0)
                || !seen.Add(id)
            )
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
                new Comment
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
                    Images = images,
                }
            );
        }

        if (sort == CommentSortOrder.Latest && items.Count > 1)
        {
            items.Sort(static (a, b) => b.CreatedAtUnixSeconds.CompareTo(a.CreatedAtUnixSeconds));
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
        string dramaId,
        string targetId,
        string text,
        bool isEpisode,
        int score,
        CancellationToken cancellationToken,
        bool danmaku = false
    )
    {
        dramaId = NormalizeDramaId(dramaId);
        targetId = isEpisode ? NormalizeVideoId(targetId) : dramaId;
        text = NormalizeText(text);
        int reqCommentSource = isEpisode ? 504 : 501;
        int reqServerChannel = isEpisode ? 18 : 34;
        int commentType =
            danmaku ? 20
            : isEpisode ? 4
            : 2;

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
                        BookId = dramaId,
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
        long createdAt =
            record.Common?.CreateTimestamp > 0
                ? record.Common.CreateTimestamp
                : record.Reply?.CreatedAt ?? 0;
        long likeCount =
            record.Stat?.DiggCount > 0 ? record.Stat.DiggCount : record.Reply?.LikeCount ?? 0;
        IReadOnlyList<CommentImage> images = MapReplyImages(record.Reply, record.Common);

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
            Images = images,
        };
    }

    private static IReadOnlyList<CommentImage> MapCommentImages(AppComment comment)
    {
        IReadOnlyList<AppCommentImageDataDto>? contentImages = comment
            .Common
            ?.Content
            ?.ImageDataList
            ?.ImageData;
        if (contentImages is { Count: > 0 })
        {
            return MapImageData(contentImages);
        }

        IReadOnlyList<AppCommentImageDto> source =
            comment.ImageList is { Count: > 0 } ? comment.ImageList
            : comment.Common?.ImageList is { Count: > 0 } ? comment.Common.ImageList
            : comment.Common?.Content?.ImageList is { Count: > 0 }
                ? comment.Common.Content.ImageList
            : comment.Expand?.ImageList ?? [];

        return MapImages(source);
    }

    private static IReadOnlyList<CommentImage> MapReplyImages(
        AppReply? reply,
        AppReplyCommon? common
    )
    {
        IReadOnlyList<AppCommentImageDataDto>? replyImages = reply
            ?.Content
            ?.ImageDataList
            ?.ImageData;
        if (replyImages is { Count: > 0 })
        {
            return MapImageData(replyImages);
        }

        IReadOnlyList<AppCommentImageDataDto>? commonImages = common
            ?.Content
            ?.ImageDataList
            ?.ImageData;
        if (commonImages is { Count: > 0 })
        {
            return MapImageData(commonImages);
        }

        IReadOnlyList<AppCommentImageDto> source =
            reply?.ImageList is { Count: > 0 } ? reply.ImageList
            : reply?.Content?.ImageList is { Count: > 0 } ? reply.Content.ImageList
            : common?.Content?.ImageList ?? [];

        return MapImages(source);
    }

    private static IReadOnlyList<CommentImage> MapImageData(
        IReadOnlyList<AppCommentImageDataDto> source
    )
    {
        List<CommentImage> images = [with(source.Count)];
        foreach (AppCommentImageDataDto raw in source)
        {
            if (raw is null)
            {
                continue;
            }

            string primaryUrl = ValueNormalizer
                .First(raw.WebUrl, raw.ExpandWebUrl, raw.WebUri)
                .Trim();
            if (primaryUrl.Length == 0)
            {
                continue;
            }

            List<string> candidateUrls = [];
            if (!string.IsNullOrWhiteSpace(raw.ExpandWebUrl))
            {
                candidateUrls.Add(raw.ExpandWebUrl.Trim());
            }
            if (!string.IsNullOrWhiteSpace(raw.WebUri))
            {
                candidateUrls.Add(raw.WebUri.Trim());
            }

            IReadOnlyList<string> backupUrls =
            [
                .. candidateUrls
                    .Where(u => !string.Equals(u, primaryUrl, StringComparison.Ordinal))
                    .Distinct(StringComparer.Ordinal),
            ];

            images.Add(
                new CommentImage
                {
                    Url = primaryUrl,
                    BackupUrls = backupUrls,
                    Width = Math.Max(0, raw.Width),
                    Height = Math.Max(0, raw.Height),
                }
            );
        }

        return images;
    }

    private static IReadOnlyList<CommentImage> MapImages(IReadOnlyList<AppCommentImageDto> source)
    {
        if (source is null || source.Count == 0)
        {
            return [];
        }

        List<CommentImage> images = [];
        foreach (AppCommentImageDto raw in source)
        {
            if (raw is null)
            {
                continue;
            }

            List<string> candidateUrls = [];
            if (raw.UrlList is { Count: > 0 })
            {
                foreach (string candidate in raw.UrlList)
                {
                    if (
                        !string.IsNullOrWhiteSpace(candidate)
                        && !candidateUrls.Contains(candidate, StringComparer.Ordinal)
                    )
                    {
                        candidateUrls.Add(candidate.Trim());
                    }
                }
            }

            if (
                !string.IsNullOrWhiteSpace(raw.Url)
                && !candidateUrls.Contains(raw.Url.Trim(), StringComparer.Ordinal)
            )
            {
                candidateUrls.Add(raw.Url.Trim());
            }

            if (raw.DownloadUrl is { Count: > 0 })
            {
                foreach (string downloadUrl in raw.DownloadUrl)
                {
                    if (
                        !string.IsNullOrWhiteSpace(downloadUrl)
                        && !candidateUrls.Contains(downloadUrl.Trim(), StringComparer.Ordinal)
                    )
                    {
                        candidateUrls.Add(downloadUrl.Trim());
                    }
                }
            }

            if (raw.DownloadUrlList is { Count: > 0 })
            {
                foreach (string downloadUrl in raw.DownloadUrlList)
                {
                    if (
                        !string.IsNullOrWhiteSpace(downloadUrl)
                        && !candidateUrls.Contains(downloadUrl.Trim(), StringComparer.Ordinal)
                    )
                    {
                        candidateUrls.Add(downloadUrl.Trim());
                    }
                }
            }

            string primaryUrl = candidateUrls.Count > 0 ? candidateUrls[0] : string.Empty;
            if (primaryUrl.Length == 0)
            {
                continue;
            }

            IReadOnlyList<string> backupUrls =
                candidateUrls.Count > 1 ? [.. candidateUrls.Skip(1)] : [];

            images.Add(
                new CommentImage
                {
                    Url = primaryUrl,
                    BackupUrls = backupUrls,
                    Width = Math.Max(0, raw.Width),
                    Height = Math.Max(0, raw.Height),
                }
            );
        }

        return images;
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
        if (value.Length is 0 or > 128)
        {
            throw new ArgumentException("评论 ID 无效。", nameof(value));
        }
        return value;
    }

    private static string NormalizeDramaOrVideoId(string value)
    {
        value = (value ?? string.Empty).Trim();
        return ValueNormalizer.IsNumericId(value)
            ? value
            : throw new ArgumentException("剧集或视频 ID 无效。", nameof(value));
    }

    private static string NormalizeText(string value)
    {
        value = SanitizeText(value ?? string.Empty);
        if (value.Length is 0 or > 4000)
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
