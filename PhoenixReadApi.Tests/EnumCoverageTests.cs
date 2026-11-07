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
            type: ShortDramaLeaderboardType.Recommend,
            channel: ShortDramaLeaderboardChannel.All,
            offset: 0,
            cancellationToken: timeout.Token
        );
        await Assert.That(allRecommend).IsNotNull();
        await Assert.That(allRecommend.Items).IsNotNull();
        await Assert.That(allRecommend.BoardTitle).IsNotNull();
        await Assert.That(allRecommend.BoardTitle).IsNotEqualTo("小卡");

        // 2. 女频 + 热播榜
        ShortDramaLeaderboardPage femaleHotPlay = await SharedScenario.Client.GetShortDramaLeaderboardAsync(
            type: ShortDramaLeaderboardType.HotPlay,
            channel: ShortDramaLeaderboardChannel.Female,
            offset: 0,
            cancellationToken: timeout.Token
        );
        await Assert.That(femaleHotPlay).IsNotNull();
        await Assert.That(femaleHotPlay.Items).IsNotNull();
        await Assert.That(femaleHotPlay.BoardTitle).Contains("热播");

        // 3. 男频 + 热播榜
        ShortDramaLeaderboardPage maleHotPlay = await SharedScenario.Client.GetShortDramaLeaderboardAsync(
            type: ShortDramaLeaderboardType.HotPlay,
            channel: ShortDramaLeaderboardChannel.Male,
            offset: 0,
            cancellationToken: timeout.Token
        );
        await Assert.That(maleHotPlay).IsNotNull();
        await Assert.That(maleHotPlay.Items).IsNotNull();
        await Assert.That(maleHotPlay.BoardTitle).Contains("热播");

        // 4. 新剧榜
        ShortDramaLeaderboardPage newReleasePage = await SharedScenario.Client.GetShortDramaLeaderboardAsync(
            type: ShortDramaLeaderboardType.NewRelease,
            channel: ShortDramaLeaderboardChannel.All,
            offset: 0,
            cancellationToken: timeout.Token
        );
        await Assert.That(newReleasePage).IsNotNull();
        await Assert.That(newReleasePage.Items).IsNotNull();
        await Assert.That(newReleasePage.BoardTitle).Contains("新剧");

        // 5. 热搜榜
        ShortDramaLeaderboardPage hotSearchPage = await SharedScenario.Client.GetShortDramaLeaderboardAsync(
            type: ShortDramaLeaderboardType.HotSearch,
            channel: ShortDramaLeaderboardChannel.All,
            offset: 0,
            cancellationToken: timeout.Token
        );
        await Assert.That(hotSearchPage).IsNotNull();
        await Assert.That(hotSearchPage.Items).IsNotNull();
        await Assert.That(hotSearchPage.BoardTitle).Contains("热搜");
    }

    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestLeaderboardDistinctResultsAcrossCategoriesAndBoards()
    {
        using CancellationTokenSource timeout = SharedScenario.CreateTimeout();

        ShortDramaLeaderboardPage femaleHotPlay = await SharedScenario.Client.GetShortDramaLeaderboardAsync(
            type: ShortDramaLeaderboardType.HotPlay,
            channel: ShortDramaLeaderboardChannel.Female,
            offset: 0,
            cancellationToken: timeout.Token
        );
        ShortDramaLeaderboardPage maleHotPlay = await SharedScenario.Client.GetShortDramaLeaderboardAsync(
            type: ShortDramaLeaderboardType.HotPlay,
            channel: ShortDramaLeaderboardChannel.Male,
            offset: 0,
            cancellationToken: timeout.Token
        );
        ShortDramaLeaderboardPage recommend = await SharedScenario.Client.GetShortDramaLeaderboardAsync(
            type: ShortDramaLeaderboardType.Recommend,
            channel: ShortDramaLeaderboardChannel.All,
            offset: 0,
            cancellationToken: timeout.Token
        );

        await Assert.That(femaleHotPlay.Items.Count).IsGreaterThan(0);
        await Assert.That(maleHotPlay.Items.Count).IsGreaterThan(0);
        await Assert.That(recommend.Items.Count).IsGreaterThan(0);

        // 校验女频与男频榜单标题包含对应受众，且内容具备区分度
        await Assert.That(femaleHotPlay.BoardTitle).Contains("女频");
        await Assert.That(maleHotPlay.BoardTitle).Contains("男频");

        string? femaleTop = femaleHotPlay.Items.FirstOrDefault()?.Id;
        string? maleTop = maleHotPlay.Items.FirstOrDefault()?.Id;
        string? recommendTop = recommend.Items.FirstOrDefault()?.Id;

        // 男频 TOP 1 与女频 TOP 1 或推荐榜 TOP 1 不全相同
        bool hasDistinction = femaleTop != maleTop || femaleTop != recommendTop;
        await Assert.That(hasDistinction).IsTrue();
    }
}
