using PhoenixReadApi.Tests.Common;

namespace PhoenixReadApi.Tests;

/// <summary>
/// 剧集详情、分集媒体播放解析与分享链接相关测试。
/// </summary>
public sealed class DetailsAndPlaybackTests
{
    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestGetDramaDetailsAsync()
    {
        using CancellationTokenSource timeout = SharedScenario.CreateTimeout();
        TestDramaScenario scenario = await SharedScenario.GetScenarioAsync(timeout.Token);

        DramaDetails details = await SharedScenario.Client.GetDramaDetailsAsync(
            scenario.SeriesId,
            timeout.Token
        );

        await Assert.That(details).IsNotNull();
        await Assert.That(details.Drama.Id).IsEqualTo(scenario.SeriesId);
        await SharedScenario.AssertValidDrama(details.Drama);

        await Assert.That(details.Episodes).IsNotNull();
        await Assert.That(details.Episodes.Count > 0).IsTrue();
        await Assert
            .That(
                details
                    .Episodes.Select(static episode => episode.Number)
                    .SequenceEqual(Enumerable.Range(1, details.Episodes.Count))
            )
            .IsTrue();

        foreach (Episode episode in details.Episodes)
        {
            await SharedScenario.AssertValidEpisode(episode);
        }
    }

    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestGetBatchDramaDetailsAsync()
    {
        using CancellationTokenSource timeout = SharedScenario.CreateTimeout();
        TestDramaScenario scenario = await SharedScenario.GetScenarioAsync(timeout.Token);

        // 批量查询单个或多个剧集
        IReadOnlyList<Drama> dramas = await SharedScenario.Client.GetBatchDramaDetailsAsync(
            [scenario.SeriesId],
            timeout.Token
        );

        await Assert.That(dramas).IsNotNull();
        await Assert.That(dramas.Count > 0).IsTrue();
        foreach (Drama drama in dramas)
        {
            await SharedScenario.AssertValidDrama(drama);
        }

        // 测试空集合输入
        IReadOnlyList<Drama> emptyResult = await SharedScenario.Client.GetBatchDramaDetailsAsync(
            [],
            timeout.Token
        );
        await Assert.That(emptyResult).IsNotNull();
        await Assert.That(emptyResult.Count).IsEqualTo(0);
    }

    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestResolveMediaAsync()
    {
        using CancellationTokenSource timeout = SharedScenario.CreateTimeout();
        TestDramaScenario scenario = await SharedScenario.GetScenarioAsync(timeout.Token);

        PlaybackMedia media = await SharedScenario.Client.ResolveMediaAsync(
            scenario.SeriesId,
            scenario.Episode.VideoId,
            timeout.Token
        );

        await SharedScenario.AssertValidPlaybackMedia(media);
    }

    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestCreateShareLinkAsync()
    {
        using CancellationTokenSource timeout = SharedScenario.CreateTimeout();
        TestDramaScenario scenario = await SharedScenario.GetScenarioAsync(timeout.Token);

        ShareLink link = await SharedScenario.Client.CreateShareLinkAsync(
            scenario.SeriesId,
            scenario.Episode.VideoId,
            timeout.Token
        );

        await Assert.That(link).IsNotNull();
        await Assert.That(link.SeriesId).IsEqualTo(scenario.SeriesId);
        await Assert.That(link.VideoId).IsEqualTo(scenario.Episode.VideoId);
        await Assert.That(link.Url).IsNotNull();
        await Assert.That(link.ShortUrl).IsNotNull();
        await Assert.That(link.Title).IsNotNull();
    }

    [Test]
    public async Task TestResolveShareLink()
    {
        const string sampleUrl = "https://example.com/share?series_id=74123456789&item_id=74987654321";
        ShareLink resolved = SharedScenario.Client.ResolveShareLink(sampleUrl);

        await Assert.That(resolved).IsNotNull();
        await Assert.That(resolved.SeriesId).IsEqualTo("74123456789");
        await Assert.That(resolved.VideoId).IsEqualTo("74987654321");
        await Assert.That(resolved.Url).IsEqualTo(sampleUrl);

        // 测试不包含参数的普通链接
        const string plainUrl = "https://example.com/drama";
        ShareLink plainResolved = SharedScenario.Client.ResolveShareLink(plainUrl);
        await Assert.That(plainResolved).IsNotNull();
        await Assert.That(plainResolved.Url).IsEqualTo(plainUrl);
        await Assert.That(plainResolved.SeriesId).IsEmpty();
        await Assert.That(plainResolved.VideoId).IsEmpty();
    }
}
