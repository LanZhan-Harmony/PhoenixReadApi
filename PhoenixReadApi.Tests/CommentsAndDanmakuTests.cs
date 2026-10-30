using PhoenixReadApi.Tests.Common;

namespace PhoenixReadApi.Tests;

/// <summary>
/// 剧评、分集评论、评论回复与弹幕相关测试。
/// </summary>
public sealed class CommentsAndDanmakuTests
{
    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestGetDanmakuAsync()
    {
        using CancellationTokenSource timeout = SharedScenario.CreateTimeout();
        TestDramaScenario scenario = await SharedScenario.GetScenarioAsync(timeout.Token);

        DanmakuPage danmaku = await SharedScenario.Client.GetDanmakuAsync(
            scenario.SeriesId,
            scenario.Episode.VideoId,
            startMs: 0,
            durationMs: 60_000,
            timeout.Token
        );

        await Assert.That(danmaku).IsNotNull();
        await Assert.That(danmaku.EpisodeId).IsEqualTo(scenario.Episode.VideoId);
        await Assert.That(danmaku.StartMs).IsEqualTo(0L);
        await Assert.That(danmaku.NextMs >= danmaku.StartMs).IsTrue();
        await Assert.That(danmaku.Total >= 0).IsTrue();
        await Assert.That(danmaku.Items).IsNotNull();

        foreach (DanmakuItem item in danmaku.Items)
        {
            await Assert.That(item).IsNotNull();
            await Assert.That(string.IsNullOrWhiteSpace(item.Id)).IsFalse();
            await Assert.That(item.Text).IsNotNull();
            await Assert
                .That(item.TimeMs >= danmaku.StartMs && item.TimeMs <= danmaku.NextMs)
                .IsTrue();
        }
    }

    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestGetDramaReviewsAsync_Hot()
    {
        using CancellationTokenSource timeout = SharedScenario.CreateTimeout();
        TestDramaScenario scenario = await SharedScenario.GetScenarioAsync(timeout.Token);

        CommentPage reviews = await SharedScenario.Client.GetDramaReviewsAsync(
            scenario.SeriesId,
            count: 5,
            sort: CommentSortOrder.Hot,
            cancellationToken: timeout.Token
        );

        await Assert.That(reviews).IsNotNull();
        await Assert.That(reviews.TargetId).IsEqualTo(scenario.SeriesId);
        await Assert.That(reviews.Items).IsNotNull();
        await Assert.That(reviews.Items.Count <= 5).IsTrue();
        await Assert.That(reviews.Total >= reviews.Items.Count).IsTrue();
        await Assert.That(reviews.Cursor).IsNotNull();

        foreach (DramaComment comment in reviews.Items)
        {
            await SharedScenario.AssertValidDramaComment(comment);
        }

        if (reviews.HasMore && reviews.Cursor.Length > 0)
        {
            CommentPage nextPage = await SharedScenario.Client.GetDramaReviewsAsync(
                scenario.SeriesId,
                count: 5,
                cursor: reviews.Cursor,
                sort: CommentSortOrder.Hot,
                cancellationToken: timeout.Token
            );
            await Assert.That(nextPage).IsNotNull();
            await Assert.That(nextPage.TargetId).IsEqualTo(scenario.SeriesId);
            await Assert.That(nextPage.Cursor).IsNotNull();
        }
    }

    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestGetDramaReviewsAsync_Latest()
    {
        using CancellationTokenSource timeout = SharedScenario.CreateTimeout();
        TestDramaScenario scenario = await SharedScenario.GetScenarioAsync(timeout.Token);

        CommentPage reviews = await SharedScenario.Client.GetDramaReviewsAsync(
            scenario.SeriesId,
            count: 5,
            sort: CommentSortOrder.Latest,
            cancellationToken: timeout.Token
        );

        await Assert.That(reviews).IsNotNull();
        await Assert.That(reviews.TargetId).IsEqualTo(scenario.SeriesId);
        await Assert.That(reviews.Items).IsNotNull();
        await Assert.That(reviews.Cursor).IsNotNull();

        foreach (DramaComment comment in reviews.Items)
        {
            await SharedScenario.AssertValidDramaComment(comment);
        }
    }

    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestGetEpisodeCommentsAsync_Hot()
    {
        using CancellationTokenSource timeout = SharedScenario.CreateTimeout();
        TestDramaScenario scenario = await SharedScenario.GetScenarioAsync(timeout.Token);

        CommentPage comments = await SharedScenario.Client.GetEpisodeCommentsAsync(
            scenario.SeriesId,
            scenario.Episode.VideoId,
            count: 5,
            sort: CommentSortOrder.Hot,
            cancellationToken: timeout.Token
        );

        await Assert.That(comments).IsNotNull();
        await Assert.That(comments.TargetId).IsEqualTo(scenario.Episode.VideoId);
        await Assert.That(comments.Items).IsNotNull();
        await Assert.That(comments.Items.Count <= 5).IsTrue();
        await Assert.That(comments.Total >= comments.Items.Count).IsTrue();
        await Assert.That(comments.Cursor).IsNotNull();

        foreach (DramaComment comment in comments.Items)
        {
            await SharedScenario.AssertValidDramaComment(comment);
        }

        if (comments.HasMore && comments.Cursor.Length > 0)
        {
            CommentPage nextPage = await SharedScenario.Client.GetEpisodeCommentsAsync(
                scenario.SeriesId,
                scenario.Episode.VideoId,
                count: 5,
                cursor: comments.Cursor,
                sort: CommentSortOrder.Hot,
                cancellationToken: timeout.Token
            );
            await Assert.That(nextPage).IsNotNull();
            await Assert.That(nextPage.TargetId).IsEqualTo(scenario.Episode.VideoId);
            await Assert.That(nextPage.Cursor).IsNotNull();
        }
    }

    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestGetEpisodeCommentsAsync_Latest()
    {
        using CancellationTokenSource timeout = SharedScenario.CreateTimeout();
        TestDramaScenario scenario = await SharedScenario.GetScenarioAsync(timeout.Token);

        CommentPage comments = await SharedScenario.Client.GetEpisodeCommentsAsync(
            scenario.SeriesId,
            scenario.Episode.VideoId,
            count: 5,
            sort: CommentSortOrder.Latest,
            cancellationToken: timeout.Token
        );

        await Assert.That(comments).IsNotNull();
        await Assert.That(comments.TargetId).IsEqualTo(scenario.Episode.VideoId);
        await Assert.That(comments.Items).IsNotNull();
        await Assert.That(comments.Cursor).IsNotNull();

        foreach (DramaComment comment in comments.Items)
        {
            await SharedScenario.AssertValidDramaComment(comment);
        }
    }

    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestGetCommentRepliesAsync()
    {
        using CancellationTokenSource timeout = SharedScenario.CreateTimeout();
        TestDramaScenario scenario = await SharedScenario.GetScenarioAsync(timeout.Token);

        // 尝试从分集评论或剧评中找到一条带有回复的评论
        CommentPage comments = await SharedScenario.Client.GetEpisodeCommentsAsync(
            scenario.SeriesId,
            scenario.Episode.VideoId,
            count: 10,
            cancellationToken: timeout.Token
        );

        DramaComment? targetComment = comments.Items.FirstOrDefault(c => c.ReplyCount > 0);
        string groupId = scenario.Episode.VideoId;

        if (targetComment is null)
        {
            CommentPage reviews = await SharedScenario.Client.GetDramaReviewsAsync(
                scenario.SeriesId,
                count: 10,
                cancellationToken: timeout.Token
            );
            targetComment =
                reviews.Items.FirstOrDefault(c => c.ReplyCount > 0)
                ?? reviews.Items.FirstOrDefault();
            groupId = scenario.SeriesId;
        }

        targetComment ??= comments.Items.FirstOrDefault();

        if (targetComment is not null && !string.IsNullOrWhiteSpace(targetComment.Id))
        {
            bool isEpisode = comments.Items.Contains(targetComment);
            // 1. 领域专用回复接口 (分集或剧评)
            CommentReplyPage repliesWithGroup = isEpisode
                ? await SharedScenario.Client.GetEpisodeCommentRepliesAsync(
                    targetComment.Id,
                    scenario.SeriesId,
                    scenario.Episode.VideoId,
                    count: 5,
                    cancellationToken: timeout.Token
                )
                : await SharedScenario.Client.GetDramaReviewRepliesAsync(
                    targetComment.Id,
                    scenario.SeriesId,
                    count: 5,
                    cancellationToken: timeout.Token
                );

            await Assert.That(repliesWithGroup).IsNotNull();
            await Assert.That(repliesWithGroup.Items).IsNotNull();
            await Assert.That(repliesWithGroup.Cursor).IsNotNull();
            await Assert.That(repliesWithGroup.Total >= 0).IsTrue();

            foreach (CommentReply reply in repliesWithGroup.Items)
            {
                await SharedScenario.AssertValidCommentReply(reply);
            }

            // 2. 通用 GetCommentRepliesAsync 重载
            CommentReplyPage replies = await SharedScenario.Client.GetCommentRepliesAsync(
                targetComment.Id,
                scenario.SeriesId,
                isEpisode ? scenario.Episode.VideoId : string.Empty,
                count: 5,
                cancellationToken: timeout.Token
            );
            await Assert.That(replies).IsNotNull();
            await Assert.That(replies.Items).IsNotNull();
        }
    }

    [Test]
    public async Task TestSendDramaReview_Validation()
    {
        // 评分范围为 0.5 到 5.0，且必须是 0.5 的倍数
        await Assert
            .That(async () =>
            {
                await SharedScenario.Client.SendDramaReviewAsync("123", rating: 0.0, "评语");
            })
            .Throws<ArgumentOutOfRangeException>();

        await Assert
            .That(async () =>
            {
                await SharedScenario.Client.SendDramaReviewAsync("123", rating: 5.5, "评语");
            })
            .Throws<ArgumentOutOfRangeException>();

        await Assert
            .That(async () =>
            {
                await SharedScenario.Client.SendDramaReviewAsync("123", rating: 3.2, "评语");
            })
            .Throws<ArgumentOutOfRangeException>();

        // 文本为空
        await Assert
            .That(async () =>
            {
                await SharedScenario.Client.SendDramaReviewAsync("123", rating: 4.5, "");
            })
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task TestCommentValidation_Arguments()
    {
        // 分集评论空文本
        await Assert
            .That(async () =>
            {
                await SharedScenario.Client.SendEpisodeCommentAsync("123", "456", "");
            })
            .Throws<ArgumentException>();

        // 弹幕空文本
        await Assert
            .That(async () =>
            {
                await SharedScenario.Client.SendDanmakuAsync("123", "456", "");
            })
            .Throws<ArgumentException>();

        // 回复空文本
        await Assert
            .That(async () =>
            {
                await SharedScenario.Client.SendCommentReplyAsync("123", "");
            })
            .Throws<ArgumentException>();
    }

    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestCommentReplyLifecycleAsync()
    {
        using PhoenixReadApiClient? authClient = SharedScenario.TryGetAuthenticatedClient();
        if (authClient is null)
        {
            return;
        }

        using CancellationTokenSource timeout = SharedScenario.CreateTimeout();
        bool validSession = await authClient.ValidateSessionAsync(timeout.Token);
        if (!validSession)
        {
            return;
        }

        TestDramaScenario scenario = await SharedScenario.GetScenarioAsync(timeout.Token);
        string marker = $"单元测试_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";

        // 1. 发送分集评论并获取返回的 Comment ID
        PhoenixOperationResult comment = await authClient.SendEpisodeCommentAsync(
            scenario.SeriesId,
            scenario.Episode.VideoId,
            marker,
            timeout.Token
        );
        await Assert.That(comment).IsNotNull();
        await Assert.That(comment.Succeeded).IsTrue();
        await Assert.That(string.IsNullOrWhiteSpace(comment.Id)).IsFalse();

        // 2. 在自己刚刚发布的评论中发表回复并获取返回的 Reply ID
        PhoenixOperationResult reply = await authClient.SendCommentReplyAsync(
            comment.Id,
            scenario.Episode.VideoId,
            $"回复_{marker}",
            "",
            timeout.Token
        );
        await Assert.That(reply).IsNotNull();
        await Assert.That(reply.Succeeded).IsTrue();
        await Assert.That(string.IsNullOrWhiteSpace(reply.Id)).IsFalse();

        // 3. 删除时先删回复！删除失败纳入测试失败范畴
        PhoenixOperationResult delReply = await authClient.DeleteReplyAsync(reply.Id, timeout.Token);
        await Assert.That(delReply).IsNotNull();
        await Assert.That(delReply.Succeeded).IsTrue();

        // 4. 再删自己发布的评论！删除失败纳入测试失败范畴
        PhoenixOperationResult delComment = await authClient.DeleteCommentAsync(
            comment.Id,
            scenario.Episode.VideoId,
            timeout.Token
        );
        await Assert.That(delComment).IsNotNull();
        await Assert.That(delComment.Succeeded).IsTrue();

        // 5. 弹幕生命周期：发送并删除
        PhoenixOperationResult danmaku = await authClient.SendDanmakuAsync(
            scenario.SeriesId,
            scenario.Episode.VideoId,
            marker,
            timeout.Token
        );
        await Assert.That(danmaku).IsNotNull();
        await Assert.That(danmaku.Succeeded).IsTrue();
        await Assert.That(string.IsNullOrWhiteSpace(danmaku.Id)).IsFalse();

        PhoenixOperationResult delDanmaku = await authClient.DeleteDanmakuAsync(
            danmaku.Id,
            scenario.Episode.VideoId,
            timeout.Token
        );
        await Assert.That(delDanmaku).IsNotNull();
        await Assert.That(delDanmaku.Succeeded).IsTrue();
    }

    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestDramaReviewLifecycleAsync()
    {
        using PhoenixReadApiClient? authClient = SharedScenario.TryGetAuthenticatedClient();
        if (authClient is null)
        {
            return;
        }

        using CancellationTokenSource timeout = SharedScenario.CreateTimeout();
        bool validSession = await authClient.ValidateSessionAsync(timeout.Token);
        if (!validSession)
        {
            return;
        }

        TestDramaScenario scenario = await SharedScenario.GetScenarioAsync(timeout.Token);
        string reviewText = $"测试剧评_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{Guid.NewGuid().ToString("N")[..6]}";

        PhoenixOperationResult review = await authClient.SendDramaReviewAsync(
            scenario.SeriesId,
            5.0,
            reviewText,
            timeout.Token
        );
        await Assert.That(review).IsNotNull();
        await Assert.That(review.Succeeded).IsTrue();
        await Assert.That(string.IsNullOrWhiteSpace(review.Id)).IsFalse();

        // 删除剧评！删除失败纳入测试失败范畴
        PhoenixOperationResult delReview = await authClient.DeleteDramaReviewAsync(
            review.Id,
            scenario.SeriesId,
            timeout.Token
        );
        await Assert.That(delReview).IsNotNull();
        await Assert.That(delReview.Succeeded).IsTrue();
    }
}
