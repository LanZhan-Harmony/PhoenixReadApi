using PhoenixReadApi.Tests.Common;

namespace PhoenixReadApi.Tests;

/// <summary>
/// 剧集追更与通知中心、表情包、负反馈与不感兴趣面板、认证区域以及演职人员/用户作品测试。
/// </summary>
public sealed class NewInteractionsAndUserTests
{
    [Test]
    public async Task TestLoginRegions()
    {
        IReadOnlyList<LoginRegion> regions = SharedScenario.Client.GetLoginRegions();
        await Assert.That(regions).IsNotNull();
        await Assert.That(regions.Count > 0).IsTrue();

        LoginRegion cn = regions.First(r => r.Code == "86");
        await Assert.That(cn.Name).IsEqualTo("中国大陆");
        await Assert.That(cn.Iso).IsEqualTo("CN");
        await Assert.That(cn.IsoCode).IsEqualTo("CN");
        await Assert.That(cn.MinLength).IsEqualTo(11);
        await Assert.That(cn.MaxLength).IsEqualTo(11);

        using CancellationTokenSource timeout = SharedScenario.CreateTimeout();
        IReadOnlyList<LoginRegion> asyncRegions = await SharedScenario.Client.GetLoginRegionsAsync(timeout.Token);
        await Assert.That(asyncRegions.Count).IsEqualTo(regions.Count);
    }

    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestDislikePanelAsync()
    {
        using CancellationTokenSource timeout = SharedScenario.CreateTimeout();
        DislikePanel panel = await SharedScenario.Client.GetDislikePanelAsync(cancellationToken: timeout.Token);

        await Assert.That(panel).IsNotNull();
        await Assert.That(panel.CardType).IsEqualTo(2);
        await Assert.That(panel.Groups).IsNotNull();
        await Assert.That(panel.Groups.Count > 0).IsTrue();

        foreach (DislikeGroup group in panel.Groups)
        {
            await Assert.That(group.Title).IsNotNull();
            await Assert.That(group.Options).IsNotNull();
            foreach (DislikeOption opt in group.Options)
            {
                await Assert.That(opt.Id > 0).IsTrue();
            }
        }
    }

    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestEmoticonsAsync()
    {
        using CancellationTokenSource timeout = SharedScenario.CreateTimeout();
        EmoticonPage page = await SharedScenario.Client.GetEmoticonsAsync(
            isCollected: false,
            offset: 0,
            count: 10,
            cancellationToken: timeout.Token
        );

        await Assert.That(page).IsNotNull();
        await Assert.That(page.Items).IsNotNull();
        await Assert.That(page.Items.Count > 0).IsTrue();

        foreach (CommentImage img in page.Items)
        {
            await Assert.That(string.IsNullOrWhiteSpace(img.Url)).IsFalse();
            await Assert.That(img.Width >= 0).IsTrue();
            await Assert.That(img.Height >= 0).IsTrue();
        }
    }

    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestFollowUpdatesAndNoticesAsync()
    {
        using CancellationTokenSource timeout = SharedScenario.CreateTimeout();

        // 匿名状态下查询追更列表（应当返回空列表或正常列表，不应崩溃）
        FollowUpdatePage updates = await SharedScenario.Client.GetFollowUpdatesAsync(
            cursor: "",
            count: 5,
            cancellationToken: timeout.Token
        );
        await Assert.That(updates).IsNotNull();
        await Assert.That(updates.Items).IsNotNull();

        // 检查通知中心
        IReadOnlyList<NoticeItem> newNotices = await SharedScenario.Client.CheckNoticesAsync(timeout.Token);
        await Assert.That(newNotices).IsNotNull();

        NoticePage notices = await SharedScenario.Client.GetNoticesAsync(timeout.Token);
        await Assert.That(notices).IsNotNull();
        await Assert.That(notices.Items).IsNotNull();
        await Assert.That(notices.UnreadCount >= 0).IsTrue();

        // 检查未追更的短剧状态
        bool isFollowing = await SharedScenario.Client.IsFollowingUpdateAsync("7699833115866303550", timeout.Token);
        await Assert.That(isFollowing).IsFalse();
    }

    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestUserProfileAndActorWorksAsync()
    {
        using CancellationTokenSource timeout = SharedScenario.CreateTimeout();
        // 黄彦恺
        string actorUserId = "3974078057440443";
        UserProfile profile = await SharedScenario.Client.GetUserProfileAsync(actorUserId, timeout.Token);

        await Assert.That(profile).IsNotNull();
        await Assert.That(profile.User.Id).IsEqualTo(actorUserId);
        await Assert.That(profile.User.Name).IsEqualTo("黄彦恺");
        await Assert.That(profile.IsVerified).IsTrue();
        await Assert.That(profile.IsCreator).IsTrue();
        await Assert.That(profile.HongguoId).IsEqualTo("Huangyankia");
        await Assert.That(profile.CreatorRoles.Count > 0).IsTrue();
        await Assert.That(profile.CreatorRoles.Contains("男演员") || profile.CreatorRoles.Contains("导演")).IsTrue();
        await Assert.That(profile.FansCount >= 0).IsTrue();
        await Assert.That(profile.FollowCount >= 0).IsTrue();
        await Assert.That(profile.LikedCount >= 0).IsTrue();
        await Assert.That(profile.Works).IsNotNull();
        await Assert.That(profile.Works.Count > 0).IsTrue();

        foreach (Drama drama in profile.Works)
        {
            await SharedScenario.AssertValidDrama(drama);
        }

        // 验证用户作品分页接口
        UserWorkPage worksPage = await SharedScenario.Client.GetUserWorksAsync(
            actorUserId,
            new UserWorksQuery
            {
                SubTab = UserWorkSubTab.All,
                Sort = UserWorkSort.Hot,
                Count = 10,
            },
            timeout.Token
        );

        await Assert.That(worksPage).IsNotNull();
        await Assert.That(worksPage.Items.Count > 0).IsTrue();
        await Assert.That(worksPage.Total >= worksPage.Items.Count).IsTrue();

        foreach (Drama drama in worksPage.Items)
        {
            await SharedScenario.AssertValidDrama(drama);
        }
    }

    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestUserFollowersAndFollowingsAsync()
    {
        using CancellationTokenSource timeout = SharedScenario.CreateTimeout();
        string actorUserId = "3974078057440443"; // 黄彦恺

        // 测试获取粉丝列表
        UserRelationPage followersPage = await SharedScenario.Client.GetUserFollowersAsync(
            actorUserId,
            count: 10,
            cancellationToken: timeout.Token
        );

        await Assert.That(followersPage).IsNotNull();
        await Assert.That(followersPage.Items).IsNotNull();
        if (!followersPage.IsPrivacyProtected)
        {
            await Assert.That(followersPage.Items.Count > 0).IsTrue();
            UserRelationItem firstFollower = followersPage.Items[0];
            await Assert.That(string.IsNullOrWhiteSpace(firstFollower.User.Id)).IsFalse();
            await Assert.That(string.IsNullOrWhiteSpace(firstFollower.User.Name)).IsFalse();
        }

        // 测试获取关注列表
        UserRelationPage followingsPage = await SharedScenario.Client.GetUserFollowingsAsync(
            actorUserId,
            count: 10,
            cancellationToken: timeout.Token
        );

        await Assert.That(followingsPage).IsNotNull();
        await Assert.That(followingsPage.Items).IsNotNull();
        if (!followingsPage.IsPrivacyProtected)
        {
            await Assert.That(followingsPage.Items.Count > 0).IsTrue();
            UserRelationItem firstFollowing = followingsPage.Items[0];
            await Assert.That(string.IsNullOrWhiteSpace(firstFollowing.User.Id)).IsFalse();
            await Assert.That(string.IsNullOrWhiteSpace(firstFollowing.User.Name)).IsFalse();
        }
    }
}
