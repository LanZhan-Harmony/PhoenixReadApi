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

        await Assert.That(profile.Activities).IsNotNull();
        await SharedScenario.AssertValidUserPageInfo(profile.ActivitiesPage);

        await Assert.That(profile.Works).IsNotNull();
        await Assert.That(profile.WorksPage).IsNotNull();

        foreach (Drama drama in profile.Collections)
        {
            await SharedScenario.AssertValidDrama(drama);
        }

        foreach (Drama drama in profile.Likes)
        {
            await SharedScenario.AssertValidDrama(drama);
        }

        foreach (UserActivity activity in profile.Activities)
        {
            await SharedScenario.AssertValidUserActivity(activity);
        }

        foreach (Drama drama in profile.Works)
        {
            await SharedScenario.AssertValidDrama(drama);
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

        foreach (Drama drama in worksPage.Items)
        {
            await SharedScenario.AssertValidDrama(drama);
        }
    }

    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestGetUserActivitiesAsync()
    {
        using CancellationTokenSource timeout = SharedScenario.CreateTimeout();
        TestDramaScenario scenario = await SharedScenario.GetScenarioAsync(timeout.Token);

        UserActivityPage activities = await SharedScenario.Client.GetUserActivitiesAsync(
            scenario.UserId,
            count: 10,
            cancellationToken: timeout.Token
        );

        await Assert.That(activities).IsNotNull();
        await Assert.That(activities.Items).IsNotNull();
        await Assert.That(activities.Cursor).IsNotNull();
        await Assert.That(activities.Total >= 0).IsTrue();

        foreach (UserActivity activity in activities.Items)
        {
            await SharedScenario.AssertValidUserActivity(activity);
        }

        if (activities is { HasMore: true, Cursor.Length: > 0 })
        {
            UserActivityPage nextPage = await SharedScenario.Client.GetUserActivitiesAsync(
                scenario.UserId,
                count: 10,
                cursor: activities.Cursor,
                cancellationToken: timeout.Token
            );

            await Assert.That(nextPage).IsNotNull();
            await Assert.That(nextPage.Items).IsNotNull();
            await Assert.That(nextPage.Cursor).IsNotNull();
        }
    }

    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestGetUserCollectionsAsync()
    {
        using CancellationTokenSource timeout = SharedScenario.CreateTimeout();
        TestDramaScenario scenario = await SharedScenario.GetScenarioAsync(timeout.Token);

        UserDramaPage collections = await SharedScenario.Client.GetUserCollectionsAsync(
            scenario.UserId,
            count: 10,
            offset: 0,
            cancellationToken: timeout.Token
        );

        await Assert.That(collections).IsNotNull();
        await Assert.That(collections.Items).IsNotNull();
        await Assert.That(collections.Cursor).IsNotNull();
        await Assert.That(collections.NextOffset >= 0).IsTrue();
        await Assert.That(collections.Total >= 0).IsTrue();

        foreach (Drama drama in collections.Items)
        {
            await SharedScenario.AssertValidDrama(drama);
        }

        if (collections is { HasMore: true, NextOffset: > 0 })
        {
            UserDramaPage nextPage = await SharedScenario.Client.GetUserCollectionsAsync(
                scenario.UserId,
                count: 10,
                offset: collections.NextOffset,
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
        await Assert.That(likes.Cursor).IsNotNull();
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
}
