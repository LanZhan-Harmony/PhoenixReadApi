using PhoenixReadApi.Internal.Contracts.App;
using PhoenixReadApi.Internal.Contracts.App.Comment;
using PhoenixReadApi.Internal.Features.Shared;
using PhoenixReadApi.Internal.Infrastructure.Runtime;

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
        CancellationToken cancellationToken
    )
    {
        var normalizedSeriesId = NormalizeSeriesId(seriesId);
        return LoadAsync(
            normalizedSeriesId,
            normalizedSeriesId,
            isEpisode: false,
            count,
            cursor,
            cancellationToken
        );
    }

    internal Task<CommentPage> GetEpisodeCommentsAsync(
        string seriesId,
        string videoId,
        int count,
        string cursor,
        CancellationToken cancellationToken
    )
    {
        var normalizedSeriesId = NormalizeSeriesId(seriesId);
        var normalizedVideoId = NormalizeVideoId(videoId);
        return LoadAsync(
            normalizedSeriesId,
            normalizedVideoId,
            isEpisode: true,
            count,
            cursor,
            cancellationToken
        );
    }

    private async Task<CommentPage> LoadAsync(
        string seriesId,
        string targetId,
        bool isEpisode,
        int count,
        string cursor,
        CancellationToken cancellationToken
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
            var groupId = comment.Common.GroupId ?? string.Empty;
            if (comment.Common.Status != "1" || (groupId.Length > 0 && groupId != targetId))
            {
                continue;
            }

            var id = (comment.Id ?? string.Empty).Trim();
            var text = SanitizeText(comment.Common.Content?.Text ?? string.Empty);
            if (id.Length == 0 || id.Length > 120 || text.Length == 0 || !seen.Add(id))
            {
                continue;
            }

            AppCommentUserInfo userInfo = comment.Common.UserInfo ?? new();
            AppCommentExpand expand = comment.Expand ?? new();
            AppCommentStat stat = comment.Stat ?? new();
            var userName = userInfo.Name ?? string.Empty;
            var userAvatar = userInfo.AvatarUrl ?? string.Empty;
            var baseInfo = userInfo.BaseInfo;
            items.Add(
                new DramaComment
                {
                    Id = id,
                    Text = text.Length > 4000 ? string.Concat(text.AsSpan(0, 4000), "…") : text,
                    Author = new User
                    {
                        Id = userInfo.Id ?? string.Empty,
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
        new string(
            value
                .Select(static c => char.IsControl(c) || c is '\u2028' or '\u2029' ? ' ' : c)
                .ToArray()
        ).Trim();
}
