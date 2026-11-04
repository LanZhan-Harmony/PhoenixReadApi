using PhoenixReadApi.Internal.Features.Shared;
using PhoenixReadApi.Internal.Serialization;
using PhoenixReadApi.Tests.Common;

namespace PhoenixReadApi.Tests;

public sealed class EnumCoverageTests
{
    [Test]
    public async Task TestMediaDefinitionEnumParsing()
    {
        // 校验各个清晰度字符串正常解析为对应枚举
        await Assert.That(ValueNormalizer.ParseDefinition(360, "360p")).IsEqualTo(MediaDefinition.P360);
        await Assert.That(ValueNormalizer.ParseDefinition(0, "360p")).IsEqualTo(MediaDefinition.P360);
        await Assert.That(ValueNormalizer.ParseDefinition(480, "480p")).IsEqualTo(MediaDefinition.P480);
        await Assert.That(ValueNormalizer.ParseDefinition(540, "540p")).IsEqualTo(MediaDefinition.P540);
        await Assert.That(ValueNormalizer.ParseDefinition(720, "720p")).IsEqualTo(MediaDefinition.P720);
        await Assert.That(ValueNormalizer.ParseDefinition(1080, "1080p")).IsEqualTo(MediaDefinition.P1080);
        await Assert.That(ValueNormalizer.ParseDefinition(2160, "2160p")).IsEqualTo(MediaDefinition.P2160);
        await Assert.That(ValueNormalizer.ParseDefinition(2160, "")).IsEqualTo(MediaDefinition.P2160);

        // 校验异常/空值解析为 Unknown
        await Assert.That(ValueNormalizer.ParseDefinition(0, null)).IsEqualTo(MediaDefinition.Unknown);
        await Assert.That(ValueNormalizer.ParseDefinition(0, "")).IsEqualTo(MediaDefinition.Unknown);
        await Assert.That(ValueNormalizer.ParseDefinition(0, "invalid_def")).IsEqualTo(MediaDefinition.Unknown);
    }

    [Test]
    public async Task TestMediaCodecEnumParsing()
    {
        // 校验各个视频编码字符串解析
        await Assert.That(ValueNormalizer.ParseCodec("h264")).IsEqualTo(MediaCodec.H264);
        await Assert.That(ValueNormalizer.ParseCodec("avc")).IsEqualTo(MediaCodec.H264);
        await Assert.That(ValueNormalizer.ParseCodec("bytevc1")).IsEqualTo(MediaCodec.ByteVC1);
        await Assert.That(ValueNormalizer.ParseCodec("h265")).IsEqualTo(MediaCodec.ByteVC1);
        await Assert.That(ValueNormalizer.ParseCodec("hevc")).IsEqualTo(MediaCodec.ByteVC1);
        await Assert.That(ValueNormalizer.ParseCodec("bytevc2")).IsEqualTo(MediaCodec.ByteVC2);
        await Assert.That(ValueNormalizer.ParseCodec("vvc")).IsEqualTo(MediaCodec.ByteVC2);
        await Assert.That(ValueNormalizer.ParseCodec("h266")).IsEqualTo(MediaCodec.ByteVC2);

        // 校验异常/空值解析为 Unknown
        await Assert.That(ValueNormalizer.ParseCodec(null)).IsEqualTo(MediaCodec.Unknown);
        await Assert.That(ValueNormalizer.ParseCodec("unknown")).IsEqualTo(MediaCodec.Unknown);
    }

    [Test]
    public async Task TestDramaReleaseStatusParsing()
    {
        // 1. 服务端数字与英文标识解析
        await Assert.That(DramaMapper.ReleaseStatusFromValue("1")).IsEqualTo(DramaReleaseStatus.Finished);
        await Assert.That(DramaMapper.ReleaseStatusFromValue("finished")).IsEqualTo(DramaReleaseStatus.Finished);
        await Assert.That(DramaMapper.ReleaseStatusFromValue("0")).IsEqualTo(DramaReleaseStatus.Ongoing);
        await Assert.That(DramaMapper.ReleaseStatusFromValue("updating")).IsEqualTo(DramaReleaseStatus.Ongoing);

        // 2. 启发式副标题解析（支持“全80集”、“已完结”、“更新至第12集”等真实格式）
        await Assert.That(DramaMapper.ReleaseStatusFromValue("", "全80集")).IsEqualTo(DramaReleaseStatus.Finished);
        await Assert.That(DramaMapper.ReleaseStatusFromValue("", "完结")).IsEqualTo(DramaReleaseStatus.Finished);
        await Assert.That(DramaMapper.ReleaseStatusFromValue("", "已完结全集")).IsEqualTo(DramaReleaseStatus.Finished);
        await Assert.That(DramaMapper.ReleaseStatusFromValue("", "更新至第25集")).IsEqualTo(DramaReleaseStatus.Ongoing);
        await Assert.That(DramaMapper.ReleaseStatusFromValue("", "连载中")).IsEqualTo(DramaReleaseStatus.Ongoing);

        // 3. 标签解析
        await Assert.That(DramaMapper.ReleaseStatusFromValue("", "", ["都市", "已完结"])).IsEqualTo(DramaReleaseStatus.Finished);
        await Assert.That(DramaMapper.ReleaseStatusFromValue("", "", ["热血", "连载"])).IsEqualTo(DramaReleaseStatus.Ongoing);

        // 4. 无有效标识回退 Unknown
        await Assert.That(DramaMapper.ReleaseStatusFromValue("", "")).IsEqualTo(DramaReleaseStatus.Unknown);
    }

    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestTagDramaSortOrderExecution()
    {
        using CancellationTokenSource timeout = SharedScenario.CreateTimeout();

        // 校验按 Hot（最热）排序
        TagDramaPage hotPage = await SharedScenario.Client.GetTagDramaPageAsync(
            sort: TagDramaSortOrder.Hot,
            count: 3,
            cancellationToken: timeout.Token
        );
        await Assert.That(hotPage).IsNotNull();
        await Assert.That(hotPage.Items).IsNotNull();

        // 校验按 Latest（最新）排序
        TagDramaPage latestPage = await SharedScenario.Client.GetTagDramaPageAsync(
            sort: TagDramaSortOrder.Latest,
            count: 3,
            cancellationToken: timeout.Token
        );
        await Assert.That(latestPage).IsNotNull();
        await Assert.That(latestPage.Items).IsNotNull();
    }

    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestShortDramaLeaderboardCategoryAndBoardExecution()
    {
        using CancellationTokenSource timeout = SharedScenario.CreateTimeout();

        // 1. 全部内容 + 推荐榜
        ShortDramaLeaderboardPage allRecommend = await SharedScenario.Client.GetShortDramaLeaderboardAsync(
            category: ShortDramaLeaderboardCategory.All,
            board: ShortDramaLeaderboardBoard.Recommend,
            offset: 0,
            cancellationToken: timeout.Token
        );
        await Assert.That(allRecommend).IsNotNull();
        await Assert.That(allRecommend.Items).IsNotNull();
        await Assert.That(allRecommend.BoardTitle).IsNotNull();

        // 2. 真人剧 + 热播榜
        ShortDramaLeaderboardPage humanHotPlay = await SharedScenario.Client.GetShortDramaLeaderboardAsync(
            category: ShortDramaLeaderboardCategory.Human,
            board: ShortDramaLeaderboardBoard.HotPlay,
            offset: 0,
            cancellationToken: timeout.Token
        );
        await Assert.That(humanHotPlay).IsNotNull();
        await Assert.That(humanHotPlay.Items).IsNotNull();

        // 3. 漫剧榜 + 默认榜
        ShortDramaLeaderboardPage comicPage = await SharedScenario.Client.GetShortDramaLeaderboardAsync(
            category: ShortDramaLeaderboardCategory.Comic,
            board: ShortDramaLeaderboardBoard.Default,
            offset: 0,
            cancellationToken: timeout.Token
        );
        await Assert.That(comicPage).IsNotNull();
        await Assert.That(comicPage.Items).IsNotNull();

        // 4. AI 剧榜
        ShortDramaLeaderboardPage aiPage = await SharedScenario.Client.GetShortDramaLeaderboardAsync(
            category: ShortDramaLeaderboardCategory.Ai,
            board: ShortDramaLeaderboardBoard.Default,
            offset: 0,
            cancellationToken: timeout.Token
        );
        await Assert.That(aiPage).IsNotNull();
        await Assert.That(aiPage.Items).IsNotNull();

        // 5. 系列剧榜
        ShortDramaLeaderboardPage seriesAlbumPage = await SharedScenario.Client.GetShortDramaLeaderboardAsync(
            category: ShortDramaLeaderboardCategory.SeriesAlbum,
            board: ShortDramaLeaderboardBoard.Recommend,
            offset: 0,
            cancellationToken: timeout.Token
        );
        await Assert.That(seriesAlbumPage).IsNotNull();
        await Assert.That(seriesAlbumPage.Items).IsNotNull();
    }
}
