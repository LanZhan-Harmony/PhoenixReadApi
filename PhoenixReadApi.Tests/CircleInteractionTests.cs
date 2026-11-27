using PhoenixReadApi.Tests.Common;

namespace PhoenixReadApi.Tests;

public sealed class CircleInteractionTests
{
    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestActorReservationSubTabAsync()
    {
        using CancellationTokenSource timeout = SharedScenario.CreateTimeout();
        string actorId = "3974078057440443"; // 黄彦恺

        UserWorkPage worksPage = await SharedScenario.Client.GetUserWorksAsync(
            actorId,
            new UserWorksQuery { SubTab = UserWorkSubTab.Reservation, Count = 10 },
            timeout.Token
        );

        await Assert.That(worksPage).IsNotNull();
        await Assert.That(worksPage.Items.Count >= 2).IsTrue();

        int dramaCount = 0;
        foreach (UserWork work in worksPage.Items)
        {
            if (work is Drama drama)
            {
                dramaCount++;
                await Assert.That(drama.Id).IsNotNull();
                await Assert.That(drama.Title).IsNotNull();
                await Assert.That(drama.ReleaseStatus).IsEqualTo(DramaReleaseStatus.Upcoming);
                await Assert.That(drama.OnlineDate).IsNotNull();
            }
        }
        await Assert.That(dramaCount >= 2).IsTrue();
    }

    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestActorReviewsSubTabAsync()
    {
        using CancellationTokenSource timeout = SharedScenario.CreateTimeout();
        string actorId = "3974078057440443"; // 黄彦恺

        UserWorkPage worksPage = await SharedScenario.Client.GetUserWorksAsync(
            actorId,
            new UserWorksQuery { SubTab = UserWorkSubTab.Review, Count = 10 },
            timeout.Token
        );

        await Assert.That(worksPage).IsNotNull();
        await Assert.That(worksPage.Items.Count >= 5).IsTrue();
        await Assert.That(worksPage.Total >= 5).IsTrue();

        int reviewCount = 0;
        foreach (UserWork work in worksPage.Items)
        {
            if (work is UserReview review)
            {
                reviewCount++;
                await Assert.That(review.Id).IsNotNull();
                await Assert.That(review.Drama).IsNotNull();
                await Assert.That(review.Drama.Title).IsNotNull();
                await Assert.That(review.Rating > 0).IsTrue();
            }
        }
        await Assert.That(reviewCount >= 5).IsTrue();
    }

    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestAuthenticatedUserPrivacyAndFavoritesAsync()
    {
        using PhoenixReadApiClient? authClient = SharedScenario.TryGetAuthenticatedClient();
        if (authClient is null)
        {
            return;
        }

        using CancellationTokenSource timeout = SharedScenario.CreateTimeout();

        // 1. 获取当前认证用户身份
        AuthenticatedUser currentUser = await authClient.GetCurrentUserInfoAsync(timeout.Token);
        await Assert.That(currentUser).IsNotNull();
        await Assert.That(currentUser.Id).IsNotNull();
        string myUserId = currentUser.Id;

        TestContext.Current?.OutputWriter.WriteLine(
            $"Current User: Id={myUserId}, Name={currentUser.Name}"
        );

        // 2. 测试 Library 领域获取自己的收藏（书架/私有资产同步）
        IReadOnlyList<Drama> favorites = await authClient.GetDramaFavoritesAsync(
            cancellationToken: timeout.Token
        );
        TestContext.Current?.OutputWriter.WriteLine(
            $"Library Drama Favorites: Count={favorites.Count}"
        );
        foreach (Drama item in favorites)
        {
            TestContext.Current?.OutputWriter.WriteLine(
                $"Fav Drama: Id={item.Id}, Title={item.Title}, Cover={item.CoverUrl}, Eps={item.EpisodeCount}"
            );
        }

        // 3. 验证 User 公开主页社交接口
        string targetUserId = "2996637849889472";
        DramaPage userFavorites = await authClient.GetUserFavoritesAsync(
            targetUserId,
            count: 10,
            cancellationToken: timeout.Token
        );
        TestContext.Current?.OutputWriter.WriteLine(
            $"User Collections: Count={userFavorites.Items.Count}, Total={userFavorites.Total}, HasMore={userFavorites.HasMore}"
        );
        foreach (var item in userFavorites.Items)
        {
            TestContext.Current?.OutputWriter.WriteLine(
                $"UserCol Item: Id={item.Id}, Title={item.Title}, Cover={item.CoverUrl}, Eps={item.EpisodeCount}"
            );
        }

        UserRelationPage followings = await authClient.GetUserFollowingsAsync(
            targetUserId,
            count: 10,
            cancellationToken: timeout.Token
        );
        await Assert.That(followings).IsNotNull();

        var appTransport = authClient.Runtime.AppTransport;
        // 打印漫剧详情原始响应
        var comicDetailsJson = await appTransport.GetRawStringAsync(
            "/novel/player/video_detail/v1",
            [new("series_id", "7655989434612993086")],
            timeout.Token
        );
        // 打印真人剧详情中的 followed 状态
        var realDetailsJson = await appTransport.GetRawStringAsync(
            "/novel/player/video_detail/v1",
            [new("series_id", "7484566994277125145")],
            timeout.Token
        );
        TestContext.Current?.OutputWriter.WriteLine(
            $"Real followed: {realDetailsJson.Contains("\"followed\":true")}"
        );
        TestContext.Current?.OutputWriter.WriteLine(
            $"Comic followed: {comicDetailsJson.Contains("\"followed\":true")}"
        );

        DramaReviewPage reviews = await authClient.GetDramaReviewsAsync(
            "7484566994277125145",
            count: 2,
            cancellationToken: timeout.Token
        );
        await Assert.That(reviews).IsNotNull();
        await Assert.That(reviews.DramaScore.HasValue).IsTrue();
        await Assert.That(reviews.DramaScore!.Value > 0).IsTrue();
        await Assert.That(reviews.ScoreCount > 0).IsTrue();
        if (reviews.AiSummary is not null)
        {
            await Assert.That(reviews.AiSummary.OverallSummary).IsNotNull();
        }
    }

    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestCirclePostCommentRepliesAndLikeActionsAsync()
    {
        using PhoenixReadApiClient? authClient = SharedScenario.TryGetAuthenticatedClient();
        if (authClient is null)
        {
            return;
        }

        using CancellationTokenSource timeout = SharedScenario.CreateTimeout();
        const string actorUserId = "3798146916562768";

        CirclePage circle = await authClient.GetUserCircleAsync(
            actorUserId,
            count: 10,
            cancellationToken: timeout.Token
        );
        Post? postItem = circle.Items
            .Select(static x => x.Value as Post)
            .FirstOrDefault(static x => x is not null);
        if (postItem is null || postItem.Id.Length == 0)
        {
            return;
        }

        CommentTarget postTarget = CommentTarget.FromPost(postItem.Id);
        CommentPage comments = await authClient.GetCommentsAsync(
            postTarget,
            count: 5,
            cancellationToken: timeout.Token
        );
        if (comments.Items.Count == 0)
        {
            return;
        }

        string commentId = comments.Items[0].Id;

        // 1. 测试获取图文评论回复列表接口
        CommentReplyPage? replies = null;
        try
        {
            replies = await authClient.GetCommentRepliesAsync(
                postTarget,
                commentId,
                count: 5,
                cancellationToken: timeout.Token
            );
        }
        catch (PhoenixReadApiException ex)
        {
            TestContext.Current?.OutputWriter.WriteLine(
                $"GetCommentRepliesAsync: {ex.Message}"
            );
        }

        if (replies is not null)
        {
            await Assert.That(replies.Items).IsNotNull();
        }

        // 2. 测试红果圈图文评论点赞接口
        PhoenixOperationResult likeResult = await authClient.SetCommentLikeAsync(
            postTarget,
            commentId,
            active: true,
            cancellationToken: timeout.Token
        );
        await Assert.That(likeResult.Succeeded).IsTrue();

        // 3. 测试完后立即取消点赞（撤销写入内容，恢复原状）
        PhoenixOperationResult unlikeResult = await authClient.SetCommentLikeAsync(
            postTarget,
            commentId,
            active: false,
            cancellationToken: timeout.Token
        );
        await Assert.That(unlikeResult.Succeeded).IsTrue();
    }
}
