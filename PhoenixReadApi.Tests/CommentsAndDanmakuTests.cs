using PhoenixReadApi.Internal.Contracts.App.Comment;
using PhoenixReadApi.Internal.Serialization;
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
        await Assert.That(danmaku.VideoId).IsEqualTo(scenario.Episode.VideoId);
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
                .That(item.OffsetMs >= danmaku.StartMs && item.OffsetMs <= danmaku.NextMs)
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

        foreach (Comment comment in reviews.Items)
        {
            await SharedScenario.AssertValidComment(comment);
        }

        if (reviews is { HasMore: true, Cursor.Length: > 0 })
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

        foreach (Comment comment in reviews.Items)
        {
            await SharedScenario.AssertValidComment(comment);
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
        await Assert.That(comments.Items.Count).IsGreaterThan(0);
        await Assert.That(comments.Total >= comments.Items.Count).IsTrue();
        await Assert.That(comments.Cursor).IsNotNull();

        foreach (Comment comment in comments.Items)
        {
            await SharedScenario.AssertValidComment(comment);
        }

        if (comments is { HasMore: true, Cursor.Length: > 0 })
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

        foreach (Comment comment in comments.Items)
        {
            await SharedScenario.AssertValidComment(comment);
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

        Comment? targetComment = comments.Items.FirstOrDefault(c => c.ReplyCount > 0);
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
                await SharedScenario.Client.SendEpisodeCommentReplyAsync("123", "456", "789", "");
            })
            .Throws<ArgumentException>();
    }

    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestCommentReplyLifecycleAsync()
    {
        using PhoenixReadApiClient? authClient = SharedScenario.TryGetAuthenticatedClient();
        await Assert.That(authClient).IsNotNull();

        using CancellationTokenSource timeout = SharedScenario.CreateTimeout();
        bool validSession = await authClient.ValidateSessionAsync(timeout.Token);
        await Assert.That(validSession).IsTrue();

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
        PhoenixOperationResult reply = await authClient.SendEpisodeCommentReplyAsync(
            comment.Id,
            scenario.SeriesId,
            scenario.Episode.VideoId,
            $"回复_{marker}",
            "",
            timeout.Token
        );
        await Assert.That(reply).IsNotNull();
        await Assert.That(reply.Succeeded).IsTrue();
        await Assert.That(string.IsNullOrWhiteSpace(reply.Id)).IsFalse();

        // 3. 删除时先删回复！删除失败纳入测试失败范畴
        PhoenixOperationResult delReply = await authClient.DeleteReplyAsync(
            reply.Id,
            timeout.Token
        );
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
        await Assert.That(authClient).IsNotNull();

        using CancellationTokenSource timeout = SharedScenario.CreateTimeout();
        bool validSession = await authClient.ValidateSessionAsync(timeout.Token);
        await Assert.That(validSession).IsTrue();

        TestDramaScenario scenario = await SharedScenario.GetScenarioAsync(timeout.Token);
        string reviewText =
            $"测试剧评_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{Guid.NewGuid().ToString("N")[..6]}";

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

    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestCommentWithImagesAndSortOrder()
    {
        using CancellationTokenSource timeout = SharedScenario.CreateTimeout();
        SearchResult search = await SharedScenario.Client.SearchAsync("我的系统不对劲，女帝心动我开挂", timeout.Token);
        var drama = search.Dramas.FirstOrDefault(d => d.Title.Contains("我的系统不对劲")) ?? search.Dramas.First();
        DramaDetails details = await SharedScenario.Client.GetDramaDetailsAsync(drama.Id, timeout.Token);
        string videoId = details.Episodes[0].VideoId;

        // 1. 测试按时间最新排序 (CommentSortOrder.Latest)
        CommentPage latestPage = await SharedScenario.Client.GetEpisodeCommentsAsync(
            drama.Id,
            videoId,
            count: 30,
            sort: CommentSortOrder.Latest,
            cancellationToken: timeout.Token
        );

        await Assert.That(latestPage).IsNotNull();
        await Assert.That(latestPage.Items.Count).IsGreaterThan(1);

        // 验证按日期排序：时间戳必须是非递增的 (降序)
        for (int i = 0; i < latestPage.Items.Count - 1; i++)
        {
            await Assert.That(latestPage.Items[i].CreatedAtUnixSeconds >= latestPage.Items[i + 1].CreatedAtUnixSeconds).IsTrue();
        }

        // 2. 验证指定带图评论 7693917255795131198 能够成功解析附图
        Comment? targetComment = latestPage.Items.FirstOrDefault(c => c.Id == "7693917255795131198");
        if (targetComment is null)
        {
            // 如果在最新一页未包含，则从最热（count: 90）获取
            CommentPage hotPage = await SharedScenario.Client.GetEpisodeCommentsAsync(
                drama.Id,
                videoId,
                count: 90,
                sort: CommentSortOrder.Hot,
                cancellationToken: timeout.Token
            );
            targetComment = hotPage.Items.FirstOrDefault(c => c.Id == "7693917255795131198");
        }

        await Assert.That(targetComment).IsNotNull();
        await Assert.That(targetComment!.Images.Count).IsGreaterThan(0);

        CommentImage image = targetComment.Images[0];
        await Assert.That(string.IsNullOrWhiteSpace(image.Url)).IsFalse();
        await Assert.That(image.Width).IsEqualTo(300);
        await Assert.That(image.Height).IsEqualTo(225);
    }

    [Test]
    public async Task TestCommentImageDataDeserialization()
    {
        // 1. 测试空对象 {}
        string emptyJson = "{}";
        AppCommentImageDataListDto? emptyDto = JsonAotSerializer.Deserialize<AppCommentImageDataListDto>(emptyJson);
        await Assert.That(emptyDto).IsNotNull();
        await Assert.That(emptyDto!.ImageData.Count).IsEqualTo(0);

        // 2. 测试空数组 []
        string arrayJson = "[]";
        AppCommentImageDataListDto? arrayDto = JsonAotSerializer.Deserialize<AppCommentImageDataListDto>(arrayJson);
        await Assert.That(arrayDto).IsNotNull();
        await Assert.That(arrayDto!.ImageData.Count).IsEqualTo(0);

        // 3. 测试包含 image_data 数组的对象
        string validJson = """
            {
                "image_data": [
                    {
                        "id": "img123",
                        "web_url": "https://p3.example.com/pic1.jpg",
                        "expand_web_url": "https://p3.example.com/expand1.jpg",
                        "web_uri": "tos-cn-i-xxx/pic1",
                        "width": 300,
                        "height": 225,
                        "format": "jpeg",
                        "image_type": 1
                    }
                ]
            }
            """;
        AppCommentImageDataListDto? validDto = JsonAotSerializer.Deserialize<AppCommentImageDataListDto>(validJson);
        await Assert.That(validDto).IsNotNull();
        await Assert.That(validDto!.ImageData.Count).IsEqualTo(1);
        AppCommentImageDataDto img = validDto.ImageData[0];
        await Assert.That(img.Id).IsEqualTo("img123");
        await Assert.That(img.WebUrl).IsEqualTo("https://p3.example.com/pic1.jpg");
        await Assert.That(img.ExpandWebUrl).IsEqualTo("https://p3.example.com/expand1.jpg");
        await Assert.That(img.Width).IsEqualTo(300);
        await Assert.That(img.Height).IsEqualTo(225);

        // 4. 测试序列化输出
        string serialized = JsonAotSerializer.Serialize(validDto);
        await Assert.That(serialized.Contains("img123")).IsTrue();
        await Assert.That(serialized.Contains("https://p3.example.com/pic1.jpg")).IsTrue();
    }
}
