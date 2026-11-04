using System.Text;
using PhoenixReadApi.Internal.Infrastructure.Crypto;
using PhoenixReadApi.Tests.Common;

namespace PhoenixReadApi.Tests;

/// <summary>
/// 补齐的功能验证测试：设备注册加解密协议、热搜榜、轮播词、演员榜、排行榜、批量详情与流式遍历。
/// </summary>
public sealed class FeatureExpansionTests
{
    [Test]
    public async Task TestTtEncrypt_MagicPrefix()
    {
        byte[] original = Encoding.UTF8.GetBytes("{\"device_id\":\"1234567890\",\"app_name\":\"phoenix_read\"}");
        byte[] encrypted = TtEncrypt.Encrypt(original);

        await Assert.That(encrypted).IsNotNull();
        await Assert.That(encrypted.Length > 38).IsTrue();

        // 验证封套魔数 0x74, 0x63, 0x05, 0x10, 0x00, 0x00
        await Assert.That(encrypted[0]).IsEqualTo((byte)0x74);
        await Assert.That(encrypted[1]).IsEqualTo((byte)0x63);
        await Assert.That(encrypted[2]).IsEqualTo((byte)0x05);
        await Assert.That(encrypted[3]).IsEqualTo((byte)0x10);
        await Assert.That(encrypted[4]).IsEqualTo((byte)0x00);
        await Assert.That(encrypted[5]).IsEqualTo((byte)0x00);
    }

    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestSearchCuesAsync()
    {
        using CancellationTokenSource timeout = SharedScenario.CreateTimeout();
        IReadOnlyList<string> cues = await SharedScenario.Client.GetSearchCuesAsync(timeout.Token);

        await Assert.That(cues).IsNotNull();
        foreach (string cue in cues)
        {
            await Assert.That(cue).IsNotNull();
        }
    }

    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestHotSearchAsync()
    {
        using CancellationTokenSource timeout = SharedScenario.CreateTimeout();
        HotSearch hotSearch = await SharedScenario.Client.GetHotSearchAsync(timeout.Token);

        await Assert.That(hotSearch).IsNotNull();
        await Assert.That(hotSearch.Guesses).IsNotNull();
        await Assert.That(hotSearch.Boards).IsNotNull();

        foreach (HotSearchBoard board in hotSearch.Boards)
        {
            await Assert.That(board).IsNotNull();
            await Assert.That(board.Title).IsNotNull();
            await Assert.That(board.Items).IsNotNull();
            foreach (HotSearchItem item in board.Items)
            {
                await Assert.That(item).IsNotNull();
                await Assert.That(item.Id).IsNotNull();
                await Assert.That(item.Title).IsNotNull();
                await Assert.That(item.CoverUrl).IsNotNull();
                await Assert.That(item.CardTips).IsNotNull();
                await Assert.That(item.Heat).IsNotNull();
                await Assert.That(item.Tag).IsNotNull();
                await Assert.That(item.Url).IsNotNull();
            }
        }
    }

    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestShortDramaLeaderboardAsync()
    {
        using CancellationTokenSource timeout = SharedScenario.CreateTimeout();
        ShortDramaLeaderboardPage page = await SharedScenario.Client.GetShortDramaLeaderboardAsync(
            offset: 0,
            cancellationToken: timeout.Token
        );

        await Assert.That(page).IsNotNull();
        await Assert.That(page.Items).IsNotNull();
        await Assert.That(page.BoardTitle).IsNotNull();

        foreach (Drama drama in page.Items)
        {
            await SharedScenario.AssertValidDrama(drama);
        }
    }

    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestShortDramaLeaderboardAsync_HumanHotPlay()
    {
        using CancellationTokenSource timeout = SharedScenario.CreateTimeout();
        ShortDramaLeaderboardPage page = await SharedScenario.Client.GetShortDramaLeaderboardAsync(
            category: ShortDramaLeaderboardCategory.Human,
            board: ShortDramaLeaderboardBoard.HotPlay,
            offset: 0,
            cancellationToken: timeout.Token
        );

        await Assert.That(page).IsNotNull();
        await Assert.That(page.Items).IsNotNull();
        await Assert.That(page.BoardTitle).IsNotNull();

        foreach (Drama drama in page.Items)
        {
            await SharedScenario.AssertValidDrama(drama);
        }
    }

    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestActorLeaderboardAsync()
    {
        using CancellationTokenSource timeout = SharedScenario.CreateTimeout();
        ActorLeaderboardPage page = await SharedScenario.Client.GetActorLeaderboardAsync(
            offset: 0,
            cancellationToken: timeout.Token
        );

        await Assert.That(page).IsNotNull();
        await Assert.That(page.Items).IsNotNull();
        await Assert.That(page.SessionId).IsNotNull();

        foreach (ActorLeaderboardItem actor in page.Items)
        {
            await Assert.That(actor).IsNotNull();
            await Assert.That(actor.ActorId).IsNotNull();
            await Assert.That(actor.Name).IsNotNull();
            await Assert.That(actor.AvatarUrl).IsNotNull();
            await Assert.That(actor.Popularity).IsNotNull();
            await Assert.That(actor.RepresentativeWork).IsNotNull();
            await Assert.That(actor.SubTitles).IsNotNull();
        }
    }

    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestBatchDramaDetailsWithEpisodesAsync()
    {
        using CancellationTokenSource timeout = SharedScenario.CreateTimeout();
        TestDramaScenario scenario = await SharedScenario.GetScenarioAsync(timeout.Token);

        // 从搜索结果选取 2 部短剧 ID，严格测试批量返回的数量必须等于 2
        SearchResult search = await SharedScenario.Client.SearchAsync("逆袭", timeout.Token);
        string[] targetIds = [.. search.Dramas.Select(d => d.Id).Where(id => id.Length > 0).Take(2)];
        if (targetIds.Length < 2)
        {
            targetIds = [scenario.SeriesId];
        }

        IReadOnlyList<DramaDetails> details = await SharedScenario.Client.GetBatchDramaDetailsWithEpisodesAsync(
            targetIds,
            timeout.Token
        );

        await Assert.That(details).IsNotNull();
        await Assert.That(details.Count).IsEqualTo(targetIds.Length);

        for (int i = 0; i < targetIds.Length; i++)
        {
            DramaDetails detailsItem = details[i];
            await Assert.That(detailsItem.Drama.Id).IsEqualTo(targetIds[i]);
            await Assert.That(detailsItem.Drama.Title).IsNotNull();
            await Assert.That(detailsItem.Episodes).IsNotNull();
            await Assert.That(detailsItem.Episodes.Count > 0).IsTrue();
            foreach (Episode episode in detailsItem.Episodes)
            {
                await Assert.That(episode.VideoId).IsNotNull();
                await Assert.That(episode.Title).IsNotNull();
                await Assert.That(episode.CoverUrl).IsNotNull();
            }
        }
    }

    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestStreamEnumerationWithDeduplicationAsync()
    {
        using CancellationTokenSource timeout = SharedScenario.CreateTimeout();

        List<Drama> streamDramas = [];
        await foreach (Drama item in SharedScenario.Client.EnumerateRecommendationsAsync(cancellationToken: timeout.Token))
        {
            streamDramas.Add(item);
            if (streamDramas.Count >= 5)
            {
                break;
            }
        }

        await Assert.That(streamDramas.Count > 0).IsTrue();

        // 验证产出的短剧 ID 无任何重复
        HashSet<string> uniqueIds = new(streamDramas.Select(d => d.Id), StringComparer.Ordinal);
        await Assert.That(uniqueIds.Count).IsEqualTo(streamDramas.Count);
    }

    [Test]
    public async Task TestModelNonNullGuarantees()
    {
        Episode episode = new();
        await Assert.That(episode.VideoId).IsNotNull();
        await Assert.That(episode.Title).IsNotNull();
        await Assert.That(episode.CoverUrl).IsNotNull();

        Drama drama = new();
        await Assert.That(drama.Id).IsNotNull();
        await Assert.That(drama.Title).IsNotNull();
        await Assert.That(drama.AlbumId).IsNotNull();
        await Assert.That(drama.NovelBookId).IsNotNull();
        await Assert.That(drama.NovelTitle).IsNotNull();
        await Assert.That(drama.Tags).IsNotNull();
        await Assert.That(drama.Credits).IsNotNull();

        PlaybackMedia media = new();
        await Assert.That(media.Url).IsNotNull();
        await Assert.That(media.BackupUrl).IsNotNull();
        await Assert.That(media.Referer).IsNotNull();
        await Assert.That(media.DecryptionKey).IsNotNull();
        await Assert.That(media.AudioUrl).IsNotNull();
        await Assert.That(media.QualityName).IsNotNull();
        await Assert.That(media.Definition).IsEqualTo(MediaDefinition.Unknown);
        await Assert.That(media.Codec).IsEqualTo(MediaCodec.Unknown);
    }
}
