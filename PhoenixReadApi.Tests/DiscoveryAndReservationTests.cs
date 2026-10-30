using PhoenixReadApi.Tests.Common;

namespace PhoenixReadApi.Tests;

/// <summary>
/// 发现、榜单频道、新剧日历与预约相关测试。
/// </summary>
public sealed class DiscoveryAndReservationTests
{
    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestGetFeedCategoriesAsync()
    {
        using CancellationTokenSource timeout = SharedScenario.CreateTimeout();

        FeedPage feed = await SharedScenario.Client.GetFeedCategoriesAsync(timeout.Token);

        await Assert.That(feed).IsNotNull();
        await Assert.That(feed.Sections).IsNotNull();
        await Assert.That(feed.Sections.Count > 0).IsTrue();
        await Assert.That(feed.SessionId).IsNotNull();
        await Assert.That(feed.NextOffset >= 0).IsTrue();

        foreach (FeedSection section in feed.Sections)
        {
            await SharedScenario.AssertValidFeedSection(section);
        }
    }

    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestGetNewSeriesAsync()
    {
        using CancellationTokenSource timeout = SharedScenario.CreateTimeout();

        FeedPage newSeries = await SharedScenario.Client.GetNewSeriesAsync(
            offset: 0,
            sessionId: "",
            cancellationToken: timeout.Token
        );

        await Assert.That(newSeries).IsNotNull();
        await Assert.That(newSeries.Sections).IsNotNull();
        await Assert.That(newSeries.SessionId).IsNotNull();
        await Assert.That(newSeries.NextOffset >= 0).IsTrue();

        foreach (FeedSection section in newSeries.Sections)
        {
            await SharedScenario.AssertValidFeedSection(section);
        }
    }

    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestGetUpcomingReservationsAsync()
    {
        using CancellationTokenSource timeout = SharedScenario.CreateTimeout();

        ReservationPage upcoming = await SharedScenario.Client.GetUpcomingReservationsAsync(
            count: 10,
            offset: 0,
            cancellationToken: timeout.Token
        );

        await Assert.That(upcoming).IsNotNull();
        await Assert.That(upcoming.Items).IsNotNull();
        await Assert.That(upcoming.NextOffset >= 0).IsTrue();
        await Assert.That(upcoming.Total >= 0).IsTrue();

        foreach (Reservation res in upcoming.Items)
        {
            await SharedScenario.AssertValidReservation(res);
        }
    }

    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestGetReservationsAsync()
    {
        using CancellationTokenSource timeout = SharedScenario.CreateTimeout();

        ReservationPage reservations = await SharedScenario.Client.GetReservationsAsync(
            count: 10,
            offset: 0,
            cancellationToken: timeout.Token
        );

        await Assert.That(reservations).IsNotNull();
        await Assert.That(reservations.Items).IsNotNull();
        await Assert.That(reservations.NextOffset >= 0).IsTrue();
        await Assert.That(reservations.Total >= 0).IsTrue();

        foreach (Reservation res in reservations.Items)
        {
            await SharedScenario.AssertValidReservation(res);
        }
    }

    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestGetCalendarAsync()
    {
        using CancellationTokenSource timeout = SharedScenario.CreateTimeout();

        ReservationPage calendar = await SharedScenario.Client.GetCalendarAsync(
            count: 10,
            offset: 0,
            cancellationToken: timeout.Token
        );

        await Assert.That(calendar).IsNotNull();
        await Assert.That(calendar.Items).IsNotNull();
        await Assert.That(calendar.NextOffset >= 0).IsTrue();
        await Assert.That(calendar.Total >= 0).IsTrue();

        foreach (Reservation res in calendar.Items)
        {
            await SharedScenario.AssertValidReservation(res);
        }
    }

    [Test]
    public async Task TestReservationOperations_Validation()
    {
        await Assert.That(async () =>
        {
            await SharedScenario.Client.SetReservationAsync("");
        }).Throws<ArgumentException>();

        await Assert.That(async () =>
        {
            await SharedScenario.Client.DeleteReservationAsync("");
        }).Throws<ArgumentException>();
    }

    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestReservationLifecycleAsync()
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

        // 1. 预约短剧
        PhoenixOperationResult setRes = await authClient.SetReservationAsync(
            scenario.SeriesId,
            true,
            timeout.Token
        );
        await Assert.That(setRes).IsNotNull();
        await Assert.That(setRes.Succeeded).IsTrue();

        // 2. 取消预约短剧
        PhoenixOperationResult delRes = await authClient.DeleteReservationAsync(
            scenario.SeriesId,
            timeout.Token
        );
        await Assert.That(delRes).IsNotNull();
        await Assert.That(delRes.Succeeded).IsTrue();
    }
}
