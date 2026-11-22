using PhoenixReadApi.Tests.Common;

namespace PhoenixReadApi.Tests;

/// <summary>
/// 用户主页与用户内容列表相关测试。
/// </summary>
public sealed class UserTests
{
    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestGetUserProfileAsync_ByUserId()
    {
        using CancellationTokenSource timeout = SharedScenario.CreateTimeout();
        TestDramaScenario scenario = await SharedScenario.GetScenarioAsync(timeout.Token);

        UserProfile profile = await SharedScenario.Client.GetUserProfileAsync(
            scenario.UserId,
            timeout.Token
        );

        await Assert.That(profile).IsNotNull();
        await Assert.That(profile.User.Id).IsEqualTo(scenario.UserId);
        await SharedScenario.AssertValidUser(profile.User);

        await Assert.That(profile.Collections).IsNotNull();
        await SharedScenario.AssertValidUserPageInfo(profile.CollectionsPage);

        await Assert.That(profile.Likes).IsNotNull();
        await SharedScenario.AssertValidUserPageInfo(profile.LikesPage);

        await Assert.That(profile.CircleItems).IsNotNull();
        await Assert.That(profile.CirclePage).IsNotNull();

        await Assert.That(profile.Works).IsNotNull();
        await Assert.That(profile.WorksPage).IsNotNull();

        await Assert.That(profile.AvailableTabs).IsNotNull();
        foreach (UserProfileTab tab in profile.AvailableTabs)
        {
            await Assert.That(tab.Name).IsNotNull();
        }

        foreach (Drama drama in profile.Collections)
        {
            await SharedScenario.AssertValidDrama(drama);
        }

        foreach (Drama drama in profile.Likes)
        {
            await SharedScenario.AssertValidDrama(drama);
        }

        foreach (CircleItem item in profile.CircleItems)
        {
            await SharedScenario.AssertValidCircleItem(item);
        }

        foreach (UserWork work in profile.Works)
        {
            if (work is Drama drama)
            {
                await SharedScenario.AssertValidDrama(drama);
            }
            else if (work is UserReview review)
            {
                await Assert.That(review.Id).IsNotNull();
            }
        }
    }

    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestGetUserProfileAsync_ByCredit()
    {
        using CancellationTokenSource timeout = SharedScenario.CreateTimeout();
        TestDramaScenario scenario = await SharedScenario.GetScenarioAsync(timeout.Token);

        UserProfile profile = await SharedScenario.Client.GetUserProfileAsync(
            scenario.Credit,
            timeout.Token
        );

        await Assert.That(profile).IsNotNull();
        await Assert.That(profile.User.Id).IsEqualTo(scenario.UserId);
        await SharedScenario.AssertValidUser(profile.User);
    }

    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestGetUserWorksAsync()
    {
        using CancellationTokenSource timeout = SharedScenario.CreateTimeout();
        TestDramaScenario scenario = await SharedScenario.GetScenarioAsync(timeout.Token);

        UserWorkPage worksPage = await SharedScenario.Client.GetUserWorksAsync(
            scenario.UserId,
            new UserWorksQuery { Count = 10 },
            timeout.Token
        );

        await Assert.That(worksPage).IsNotNull();
        await Assert.That(worksPage.Items).IsNotNull();
        await Assert.That(worksPage.Cursor).IsNotNull();
        await Assert.That(worksPage.Total >= 0).IsTrue();

        foreach (UserWork work in worksPage.Items)
        {
            if (work is Drama drama)
            {
                await SharedScenario.AssertValidDrama(drama);
            }
            else if (work is UserReview review)
            {
                await Assert.That(review.Id).IsNotNull();
            }
        }
    }

    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestGetUserCircleAsync()
    {
        using CancellationTokenSource timeout = SharedScenario.CreateTimeout();
        // 使用具有红果圈的代表性演员 ID（如何聪睿 3798146916562768）
        const string actorUserId = "3798146916562768";

        CirclePage circlePage = await SharedScenario.Client.GetUserCircleAsync(
            actorUserId,
            count: 10,
            cancellationToken: timeout.Token
        );

        await Assert.That(circlePage).IsNotNull();
        await Assert.That(circlePage.Items).IsNotNull();
        await Assert.That(circlePage.Cursor).IsNotNull();

        foreach (CircleItem item in circlePage.Items)
        {
            await SharedScenario.AssertValidCircleItem(item);
        }

        CircleItem? postItem = circlePage.Items.FirstOrDefault(static x =>
            x.Type == CircleItemType.Post
        );
        if (postItem is not null && postItem.Id.Length > 0)
        {
            CirclePost post = await SharedScenario.Client.GetCirclePostAsync(
                postItem.Id,
                timeout.Token
            );
            await SharedScenario.AssertValidCirclePost(post);

            CommentPage postComments = await SharedScenario.Client.GetCirclePostCommentsAsync(
                postItem.Id,
                count: 5,
                cancellationToken: timeout.Token
            );
            await Assert.That(postComments).IsNotNull();
            await Assert.That(postComments.Items).IsNotNull();
            TestContext.Current?.OutputWriter.WriteLine(
                $"PostId={postItem.Id}, Comments={postComments.Items.Count}"
            );
            if (postComments.Items.Count > 0)
            {
                TestContext.Current?.OutputWriter.WriteLine(
                    $"First Comment: Id={postComments.Items[0].Id}, ReplyCount={postComments.Items[0].ReplyCount}, Text={postComments.Items[0].Text}"
                );
            }
        }

        CircleItem? videoItem = circlePage.Items.FirstOrDefault(static x =>
            x.Type == CircleItemType.Video
        );
        if (videoItem is not null && videoItem.Id.Length > 0)
        {
            CircleVideo video = await SharedScenario.Client.GetCircleVideoAsync(
                videoItem.Id,
                timeout.Token
            );
            await SharedScenario.AssertValidCircleVideo(video);

            CommentPage videoComments = await SharedScenario.Client.GetCircleVideoCommentsAsync(
                videoItem.Id,
                count: 5,
                cancellationToken: timeout.Token
            );
            await Assert.That(videoComments).IsNotNull();
            await Assert.That(videoComments.Items).IsNotNull();
        }
    }

    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestGetUserFavoritesAsync()
    {
        using CancellationTokenSource timeout = SharedScenario.CreateTimeout();
        TestDramaScenario scenario = await SharedScenario.GetScenarioAsync(timeout.Token);

        DramaPage favorites = await SharedScenario.Client.GetUserFavoritesAsync(
            scenario.UserId,
            count: 10,
            offset: 0,
            cancellationToken: timeout.Token
        );

        await Assert.That(favorites).IsNotNull();
        await Assert.That(favorites.Items).IsNotNull();
        await Assert.That(favorites.NextOffset >= 0).IsTrue();
        await Assert.That(favorites.Total >= 0).IsTrue();

        foreach (Drama drama in favorites.Items)
        {
            await SharedScenario.AssertValidDrama(drama);
        }

        if (favorites is { HasMore: true, NextOffset: > 0 })
        {
            DramaPage nextPage = await SharedScenario.Client.GetUserFavoritesAsync(
                scenario.UserId,
                count: 10,
                offset: favorites.NextOffset,
                cancellationToken: timeout.Token
            );

            await Assert.That(nextPage).IsNotNull();
            await Assert.That(nextPage.Items).IsNotNull();
        }
    }

    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestGetUserLikesAsync()
    {
        using CancellationTokenSource timeout = SharedScenario.CreateTimeout();
        TestDramaScenario scenario = await SharedScenario.GetScenarioAsync(timeout.Token);

        UserDramaPage likes = await SharedScenario.Client.GetUserLikesAsync(
            scenario.UserId,
            count: 10,
            offset: 0,
            cancellationToken: timeout.Token
        );

        await Assert.That(likes).IsNotNull();
        await Assert.That(likes.Items).IsNotNull();
        await Assert.That(likes.NextOffset >= 0).IsTrue();
        await Assert.That(likes.Total >= 0).IsTrue();

        foreach (Drama drama in likes.Items)
        {
            await SharedScenario.AssertValidDrama(drama);
        }

        if (likes is { HasMore: true, NextOffset: > 0 })
        {
            UserDramaPage nextPage = await SharedScenario.Client.GetUserLikesAsync(
                scenario.UserId,
                count: 10,
                offset: likes.NextOffset,
                cancellationToken: timeout.Token
            );

            await Assert.That(nextPage).IsNotNull();
            await Assert.That(nextPage.Items).IsNotNull();
        }
    }

    [Test]
    public async Task TestUserProfileTabTypeEnumValues()
    {
        UserProfileTabType unknown = UserProfileTabType.Unknown;
        UserProfileTabType circle = UserProfileTabType.Circle;
        UserProfileTabType like = UserProfileTabType.Like;
        UserProfileTabType favorite = UserProfileTabType.Favorite;
        UserProfileTabType work = UserProfileTabType.Work;

        await Assert.That((int)unknown).IsEqualTo(0);
        await Assert.That((int)circle).IsEqualTo(7);
        await Assert.That((int)like).IsEqualTo(9);
        await Assert.That((int)favorite).IsEqualTo(10);
        await Assert.That((int)work).IsEqualTo(23);
    }
}
