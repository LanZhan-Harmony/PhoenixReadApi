using PhoenixReadApi.Tests.Common;

namespace PhoenixReadApi.Tests;

/// <summary>
/// 历史记录、播放进度、收藏、点赞及关注动态等书架功能测试。
/// </summary>
public sealed class LibraryTests
{
    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestGetHistoryAsync()
    {
        using CancellationTokenSource timeout = SharedScenario.CreateTimeout();

        ReadHistoryPage history = await SharedScenario.Client.GetHistoryAsync(
            count: 10,
            offset: 0,
            cancellationToken: timeout.Token
        );

        await Assert.That(history).IsNotNull();
        await Assert.That(history.Items).IsNotNull();
        await Assert.That(history.NextOffset >= 0).IsTrue();
        await Assert.That(history.Total >= 0).IsTrue();

        foreach (ReadHistoryEntry entry in history.Items)
        {
            await SharedScenario.AssertValidReadHistoryEntry(entry);
        }
    }

    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestGetReadProgressAsync()
    {
        using CancellationTokenSource timeout = SharedScenario.CreateTimeout();

        ReadProgressPage progress = await SharedScenario.Client.GetReadProgressAsync(timeout.Token);

        await Assert.That(progress).IsNotNull();
        await Assert.That(progress.Items).IsNotNull();
        await Assert.That(progress.LocalItems).IsNotNull();

        foreach (ReadProgress item in progress.Items)
        {
            await SharedScenario.AssertValidReadProgress(item);
        }
    }

    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestGetReadProgressForAsync()
    {
        using CancellationTokenSource timeout = SharedScenario.CreateTimeout();
        TestDramaScenario scenario = await SharedScenario.GetScenarioAsync(timeout.Token);

        ReadProgressPage progress = await SharedScenario.Client.GetReadProgressForAsync(
            [scenario.SeriesId],
            timeout.Token
        );

        await Assert.That(progress).IsNotNull();
        await Assert.That(progress.Items).IsNotNull();
        await Assert.That(progress.LocalItems).IsNotNull();

        foreach (ReadProgress item in progress.Items)
        {
            await SharedScenario.AssertValidReadProgress(item);
        }

        // 测试空参数校验
        await Assert.That(async () =>
        {
            await SharedScenario.Client.GetReadProgressForAsync([]);
        }).Throws<ArgumentException>();
    }

    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestGetFavoritesAsync_And_GetFavoriteIdsAsync()
    {
        using CancellationTokenSource timeout = SharedScenario.CreateTimeout();

        DramaPage favorites = await SharedScenario.Client.GetFavoritesAsync(
            count: 10,
            offset: 0,
            cancellationToken: timeout.Token
        );

        await Assert.That(favorites).IsNotNull();
        await Assert.That(favorites.Items).IsNotNull();
        await Assert.That(favorites.Cursor).IsNotNull();
        await Assert.That(favorites.NextOffset >= 0).IsTrue();
        await Assert.That(favorites.Total >= 0).IsTrue();

        foreach (Drama drama in favorites.Items)
        {
            await SharedScenario.AssertValidDrama(drama, allowSparse: true);
        }

        // 测试 GetFavoriteIdsAsync 元组重载
        (IReadOnlyList<string> ids, DramaPage page) = await SharedScenario.Client.GetFavoriteIdsAsync(
            count: 10,
            offset: 0,
            cancellationToken: timeout.Token
        );

        await Assert.That(ids).IsNotNull();
        await Assert.That(page).IsNotNull();
        await Assert.That(ids.Count).IsEqualTo(page.Items.Count);
    }

    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestGetLikesAsync()
    {
        using CancellationTokenSource timeout = SharedScenario.CreateTimeout();

        DramaPage likes = await SharedScenario.Client.GetLikesAsync(
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
            await SharedScenario.AssertValidDrama(drama, allowSparse: true);
        }
    }

    [Test]
    public async Task TestLibraryOperations_Validation()
    {
        // ReportProgress 参数空校验
        await Assert.That(async () =>
        {
            await SharedScenario.Client.ReportProgressAsync(new ProgressUpdate { SeriesId = "", VideoId = "" });
        }).Throws<ArgumentException>();

        // DeleteHistory 参数空校验
        await Assert.That(async () =>
        {
            await SharedScenario.Client.DeleteHistoryAsync("", "");
        }).Throws<ArgumentException>();

        // SetFavorite 参数空校验
        await Assert.That(async () =>
        {
            await SharedScenario.Client.SetFavoriteAsync("", "");
        }).Throws<ArgumentException>();

        // RemoveFavorite 参数空校验
        await Assert.That(async () =>
        {
            await SharedScenario.Client.RemoveFavoriteAsync("", "");
        }).Throws<ArgumentException>();

        // SetFollow 参数空校验
        await Assert.That(async () =>
        {
            await SharedScenario.Client.SetFollowAsync("");
        }).Throws<ArgumentException>();

        // SetDramaEpisodeLike 参数空校验
        await Assert.That(async () =>
        {
            await SharedScenario.Client.SetDramaEpisodeLikeAsync("", "");
        }).Throws<ArgumentException>();
    }

    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestDramaEpisodeLikeLifecycleAsync()
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

        // 1. 点赞短剧分集
        PhoenixOperationResult likeResult = await authClient.SetDramaEpisodeLikeAsync(
            scenario.SeriesId,
            scenario.Episode.VideoId,
            true,
            timeout.Token
        );
        await Assert.That(likeResult).IsNotNull();
        await Assert.That(likeResult.Succeeded).IsTrue();

        // 2. 取消点赞分集，必须成功返回并断言
        PhoenixOperationResult cancelLikeResult = await authClient.SetDramaEpisodeLikeAsync(
            scenario.SeriesId,
            scenario.Episode.VideoId,
            false,
            timeout.Token
        );
        await Assert.That(cancelLikeResult).IsNotNull();
        await Assert.That(cancelLikeResult.Succeeded).IsTrue();
    }

    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestFavoritesLifecycleAsync()
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

        // 1. 收藏短剧
        PhoenixOperationResult favResult = await authClient.SetFavoriteAsync(
            scenario.SeriesId,
            scenario.Episode.VideoId,
            true,
            timeout.Token
        );
        await Assert.That(favResult).IsNotNull();
        await Assert.That(favResult.Succeeded).IsTrue();

        // 2. 移除收藏
        PhoenixOperationResult removeResult = await authClient.RemoveFavoriteAsync(
            scenario.SeriesId,
            scenario.Episode.VideoId,
            timeout.Token
        );
        await Assert.That(removeResult).IsNotNull();
        await Assert.That(removeResult.Succeeded).IsTrue();
    }

    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestFollowLifecycleAsync()
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

        // 1. 关注用户
        PhoenixOperationResult followResult = await authClient.SetFollowAsync(
            SharedScenario.FallbackUserId,
            true,
            timeout.Token
        );
        await Assert.That(followResult).IsNotNull();
        await Assert.That(followResult.Succeeded).IsTrue();

        // 2. 取消关注用户（清理状态）
        PhoenixOperationResult unfollowResult = await authClient.SetFollowAsync(
            SharedScenario.FallbackUserId,
            false,
            timeout.Token
        );
        await Assert.That(unfollowResult).IsNotNull();
        await Assert.That(unfollowResult.Succeeded).IsTrue();
    }

    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestHistoryAndProgressLifecycleAsync()
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

        // 1. 上报播放进度
        PhoenixOperationResult reportResult = await authClient.ReportProgressAsync(
            new ProgressUpdate
            {
                SeriesId = scenario.SeriesId,
                VideoId = scenario.Episode.VideoId,
                EpisodeNumber = scenario.Episode.Number,
                CurrentPositionMs = 5000,
                DurationMs = 60000,
            },
            timeout.Token
        );
        await Assert.That(reportResult).IsNotNull();
        await Assert.That(reportResult.Succeeded).IsTrue();

        // 2. 删除历史记录
        PhoenixOperationResult delHistory = await authClient.DeleteHistoryAsync(
            scenario.SeriesId,
            scenario.Episode.VideoId,
            timeout.Token
        );
        await Assert.That(delHistory).IsNotNull();
        await Assert.That(delHistory.Succeeded).IsTrue();
    }
}
