using PhoenixReadApi.Tests.Common;

namespace PhoenixReadApi.Tests;

/// <summary>
/// 发现、榜单频道、新剧日历与预约相关测试。
/// </summary>
public sealed class DiscoveryAndReservationTests
{
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

        foreach (ReservationItem res in upcoming.Items)
        {
            await SharedScenario.AssertValidReservation(res);
        }
    }

    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestGetPendingReservationsAsync()
    {
        using CancellationTokenSource timeout = SharedScenario.CreateTimeout();

        ReservationPage reservations = await SharedScenario.Client.GetPendingReservationsAsync(
            count: 10,
            offset: 0,
            cancellationToken: timeout.Token
        );

        await Assert.That(reservations).IsNotNull();
        await Assert.That(reservations.Items).IsNotNull();
        await Assert.That(reservations.NextOffset >= 0).IsTrue();
        await Assert.That(reservations.Total >= 0).IsTrue();

        foreach (ReservationItem res in reservations.Items)
        {
            await SharedScenario.AssertValidReservation(res);
        }
    }

    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestGetAvailableReservationsAsync()
    {
        using CancellationTokenSource timeout = SharedScenario.CreateTimeout();

        ReservationPage reservations = await SharedScenario.Client.GetAvailableReservationsAsync(
            count: 10,
            offset: 0,
            cancellationToken: timeout.Token
        );

        await Assert.That(reservations).IsNotNull();
        await Assert.That(reservations.Items).IsNotNull();
        await Assert.That(reservations.NextOffset >= 0).IsTrue();
        await Assert.That(reservations.Total >= 0).IsTrue();

        foreach (ReservationItem res in reservations.Items)
        {
            await SharedScenario.AssertValidReservation(res);
        }
    }

    [Test]
    public async Task TestReservationOperations_Validation()
    {
        await Assert
            .That(async () =>
            {
                await SharedScenario.Client.SetReservationAsync("");
            })
            .Throws<ArgumentException>();

        await Assert
            .That(async () =>
            {
                await SharedScenario.Client.DeleteReservationAsync("");
            })
            .Throws<ArgumentException>();
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
            scenario.DramaId,
            true,
            timeout.Token
        );
        await Assert.That(setRes).IsNotNull();
        await Assert.That(setRes.Succeeded).IsTrue();

        // 2. 取消预约短剧
        PhoenixOperationResult delRes = await authClient.DeleteReservationAsync(
            scenario.DramaId,
            timeout.Token
        );
        await Assert.That(delRes).IsNotNull();
        await Assert.That(delRes.Succeeded).IsTrue();
    }
}
