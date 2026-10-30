namespace PhoenixReadApi.Tests.Common;

/// <summary>
/// 测试套件共享的客户端上下文、场景数据缓存和全属性校验辅助方法。
/// </summary>
public static class SharedScenario
{
    public const string DefaultKeyword = "请君入我怀";
    public const string FallbackUserId = "3112051666323866";

    /// <summary>
    /// 全局测试共享的客户端实例，配置合理的重试与超时。
    /// </summary>
    public static readonly PhoenixReadApiClient Client = new(
        new PhoenixReadApiOptions
        {
            MaxPagesPerCategory = 1,
            RequestTimeout = TimeSpan.FromSeconds(25),
            RetryCount = 2,
            CompleteSearchSeasons = false,
        }
    );

    /// <summary>如果存在本地开发环境的会话文件，获取带登录态的测试客户端。</summary>
    public static PhoenixReadApiClient? TryGetAuthenticatedClient()
    {
        string candidate = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "PhoenixReadApi.Sample", ".auth", "phone-session.json");
        string fullPath = Path.GetFullPath(candidate);
        if (File.Exists(fullPath))
        {
            return new PhoenixReadApiClient(
                new PhoenixReadApiOptions
                {
                    MaxPagesPerCategory = 1,
                    RequestTimeout = TimeSpan.FromSeconds(25),
                    RetryCount = 1,
                    CompleteSearchSeasons = false,
                    AuthenticationStateFilePath = fullPath,
                }
            );
        }
        return null;
    }

    private static readonly Lazy<Task<TestDramaScenario>> ScenarioTask = new(
        CreateScenarioAsync,
        LazyThreadSafetyMode.ExecutionAndPublication
    );

    /// <summary>创建默认超时的 CancellationTokenSource。</summary>
    public static CancellationTokenSource CreateTimeout(int minutes = 2) =>
        new(TimeSpan.FromMinutes(minutes));

    /// <summary>获取全局复用的有效剧集场景。</summary>
    public static async Task<TestDramaScenario> GetScenarioAsync(CancellationToken cancellationToken) =>
        await ScenarioTask.Value.WaitAsync(cancellationToken);

    private static async Task<TestDramaScenario> CreateScenarioAsync()
    {
        using CancellationTokenSource timeout = new(TimeSpan.FromMinutes(4));
        SearchResult search = await Client.SearchAsync(DefaultKeyword, timeout.Token);
        Drama? selected = search.Items.FirstOrDefault(item =>
            string.Equals(item.Title, DefaultKeyword, StringComparison.OrdinalIgnoreCase)
        );
        selected ??= search.Items.FirstOrDefault(item =>
            item.Title.Contains(DefaultKeyword, StringComparison.OrdinalIgnoreCase)
        );
        selected ??= search.Items.FirstOrDefault(IsValidDramaItem);

        if (selected is null)
        {
            IReadOnlyList<Drama> catalog = await Client.GetCatalogAsync(
                PhoenixReadApiCategory.AiDrama,
                timeout.Token
            );
            selected = catalog.FirstOrDefault(IsValidDramaItem);
        }

        Drama selectedDrama =
            selected ?? throw new InvalidOperationException("搜索和剧库都没有返回可用剧集以供初始化测试场景。");

        DramaDetails details = await Client.GetDramaDetailsAsync(selectedDrama.Id, timeout.Token);
        Episode episode =
            details.Episodes.FirstOrDefault(IsValidEpisodeItem)
            ?? throw new InvalidOperationException("剧集详情没有返回可用分集。");

        DramaCredit? credit = details.Drama.Credits.FirstOrDefault(item =>
            IsNumericId(item.User.Id)
        );
        string userId = credit?.User.Id ?? FallbackUserId;
        credit ??= new DramaCredit { User = new User { Id = userId } };

        return new TestDramaScenario(details.Drama.Id, episode, credit, userId);
    }

    private static bool IsValidDramaItem(Drama drama) =>
        IsNumericId(drama.Id) && !string.IsNullOrWhiteSpace(drama.Title);

    private static bool IsValidEpisodeItem(Episode episode) =>
        episode.Number > 0 && IsNumericId(episode.VideoId);

    public static bool IsNumericId(string? value) =>
        !string.IsNullOrEmpty(value) && value.All(static c => c is >= '0' and <= '9');

    public static bool IsHttpUrl(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && Uri.TryCreate(value, UriKind.Absolute, out Uri? uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    // =========================================================================
    // 全属性校验断言工具方法
    // =========================================================================

    public static async Task AssertValidDrama(Drama? drama, bool allowSparse = false)
    {
        await Assert.That(drama).IsNotNull();
        await Assert.That(IsNumericId(drama!.Id)).IsTrue();
        await Assert.That(drama.Title).IsNotNull();
        await Assert.That(drama.Description).IsNotNull();
        await Assert.That(drama.CoverUrl).IsNotNull();
        await Assert.That(drama.Category).IsNotNull();
        await Assert.That(drama.Remark).IsNotNull();
        await Assert.That(drama.Score).IsNotNull();
        await Assert.That(drama.Views).IsNotNull();
        await Assert.That(drama.Heat).IsNotNull();
        await Assert.That(drama.OnlineDate).IsNotNull();
        await Assert.That(drama.EpisodeCount >= 0).IsTrue();
        await Assert.That(Enum.IsDefined(typeof(DramaReleaseStatus), drama.ReleaseStatus)).IsTrue();
        await Assert.That(drama.Tags).IsNotNull();
        await Assert.That(drama.Credits).IsNotNull();

        if (!allowSparse)
        {
            await Assert.That(string.IsNullOrWhiteSpace(drama.Title)).IsFalse();
        }

        foreach (DramaCredit credit in drama.Credits)
        {
            await AssertValidDramaCredit(credit);
        }
    }

    public static async Task AssertValidDramaCredit(DramaCredit? credit)
    {
        await Assert.That(credit).IsNotNull();
        await AssertValidUser(credit!.User);
        await Assert.That(credit.Roles).IsNotNull();
        foreach (DramaCreditRole role in credit.Roles)
        {
            await Assert.That(role).IsNotNull();
            await Assert.That(role.CharacterName).IsNotNull();
            await Assert.That(role.Label).IsNotNull();
            await Assert.That(Enum.IsDefined(typeof(DramaCreditRoleType), role.Type)).IsTrue();
        }
    }

    public static async Task AssertValidUser(User? user)
    {
        await Assert.That(user).IsNotNull();
        await Assert.That(user!.Id).IsNotNull();
        await Assert.That(user.Name).IsNotNull();
        await Assert.That(user.Introduction).IsNotNull();
        await Assert.That(user.AvatarUrl).IsNotNull();
    }

    public static async Task AssertValidEpisode(Episode? episode)
    {
        await Assert.That(episode).IsNotNull();
        await Assert.That(episode!.Number > 0).IsTrue();
        await Assert.That(IsNumericId(episode.VideoId)).IsTrue();
        await Assert.That(episode.Title).IsNotNull();
    }

    public static async Task AssertValidDramaComment(DramaComment? comment)
    {
        await Assert.That(comment).IsNotNull();
        await Assert.That(string.IsNullOrWhiteSpace(comment!.Id)).IsFalse();
        await Assert.That(comment.Text).IsNotNull();
        await AssertValidUser(comment.Author);
        await Assert.That(comment.CreatedAtUnixSeconds >= 0).IsTrue();
        await Assert.That(comment.OffsetMs >= 0).IsTrue();
        await Assert.That(comment.LikeCount >= 0).IsTrue();
        await Assert.That(comment.ReplyCount >= 0).IsTrue();
    }

    public static async Task AssertValidCommentReply(CommentReply? reply)
    {
        await Assert.That(reply).IsNotNull();
        await Assert.That(string.IsNullOrWhiteSpace(reply!.Id)).IsFalse();
        await Assert.That(reply.CommentId).IsNotNull();
        await Assert.That(reply.ReplyToUserId).IsNotNull();
        await Assert.That(reply.Text).IsNotNull();
        await AssertValidUser(reply.Author);
        await Assert.That(reply.CreatedAtUnixSeconds >= 0).IsTrue();
        await Assert.That(reply.LikeCount >= 0).IsTrue();
    }

    public static async Task AssertValidPlaybackMedia(PlaybackMedia? media)
    {
        await Assert.That(media).IsNotNull();
        await Assert.That(IsHttpUrl(media!.Url)).IsTrue();
        await Assert.That(media.Referer).IsNotNull();
        await Assert.That(media.Duration >= TimeSpan.Zero).IsTrue();
        await Assert.That(media.Quality >= 0).IsTrue();
        await Assert.That(media.DecryptionKey).IsNotNull();
        await Assert.That(media.Variants).IsNotNull();
        await Assert.That(media.Variants.Count > 0).IsTrue();

        foreach (MediaVariant variant in media.Variants)
        {
            await Assert.That(variant).IsNotNull();
            await Assert.That(IsHttpUrl(variant.Url)).IsTrue();
            await Assert.That(variant.Quality >= 0).IsTrue();
            await Assert.That(variant.DecryptionKey).IsNotNull();
        }
    }

    public static async Task AssertValidTagCategoryGroup(TagCategoryGroup? group)
    {
        await Assert.That(group).IsNotNull();
        await Assert.That(string.IsNullOrWhiteSpace(group!.Name)).IsFalse();
        await Assert.That(group.Items).IsNotNull();
        foreach (TagCategoryItem item in group.Items)
        {
            await Assert.That(item).IsNotNull();
            await Assert.That(item.Id).IsNotNull();
            await Assert.That(item.Name).IsNotNull();
        }
    }

    public static async Task AssertValidTagDramaPage(TagDramaPage? page)
    {
        await Assert.That(page).IsNotNull();
        await Assert.That(page!.Items).IsNotNull();
        await Assert.That(page.NextOffset >= 0).IsTrue();
        await Assert.That(page.SessionId).IsNotNull();
        foreach (Drama drama in page.Items)
        {
            await AssertValidDrama(drama);
        }
    }

    public static async Task AssertValidFeedSection(FeedSection? section)
    {
        await Assert.That(section).IsNotNull();
        await Assert.That(section!.Id).IsNotNull();
        await Assert.That(section.Title).IsNotNull();
        await Assert.That(section.Items).IsNotNull();
        foreach (Drama drama in section.Items)
        {
            await AssertValidDrama(drama);
        }
    }

    public static async Task AssertValidReservation(Reservation? reservation)
    {
        await Assert.That(reservation).IsNotNull();
        await Assert.That(reservation!.Id).IsNotNull();
        await Assert.That(IsNumericId(reservation.SeriesId)).IsTrue();
        await Assert.That(reservation.VideoId).IsNotNull();
        await Assert.That(reservation.Title).IsNotNull();
        await Assert.That(reservation.CoverUrl).IsNotNull();
        await Assert.That(reservation.ReservedAtUnixSeconds >= 0).IsTrue();
        await Assert.That(reservation.ReleaseAtUnixSeconds >= 0).IsTrue();
    }

    public static async Task AssertValidReadHistoryEntry(ReadHistoryEntry? entry)
    {
        await Assert.That(entry).IsNotNull();
        await Assert.That(entry!.Id).IsNotNull();
        await Assert.That(IsNumericId(entry.SeriesId)).IsTrue();
        await Assert.That(entry.VideoId).IsNotNull();
        await Assert.That(entry.EpisodeNumber >= 0).IsTrue();
        await Assert.That(entry.Title).IsNotNull();
        await Assert.That(entry.CoverUrl).IsNotNull();
        await Assert.That(entry.CurrentPositionMs >= 0).IsTrue();
        await Assert.That(entry.DurationMs >= 0).IsTrue();
        await Assert.That(entry.UpdatedAtUnixMilliseconds >= 0).IsTrue();
    }

    public static async Task AssertValidReadProgress(ReadProgress? progress)
    {
        await Assert.That(progress).IsNotNull();
        await Assert.That(IsNumericId(progress!.SeriesId)).IsTrue();
        await Assert.That(progress.VideoId).IsNotNull();
        await Assert.That(progress.EpisodeNumber >= 0).IsTrue();
        await Assert.That(progress.EpisodeCount >= 0).IsTrue();
        await Assert.That(progress.LastWatchTimeUnixMilliseconds >= 0).IsTrue();
        await Assert.That(progress.CurrentPositionMs >= 0).IsTrue();
        await Assert.That(progress.DurationMs >= 0).IsTrue();
        await Assert.That(progress.CumulativeDurationMs >= 0).IsTrue();
    }

    public static async Task AssertValidUserActivity(UserActivity? activity)
    {
        await Assert.That(activity).IsNotNull();
        await Assert.That(activity!.Id).IsNotNull();
        await Assert.That(activity.Text).IsNotNull();
        await Assert.That(activity.CoverUrl).IsNotNull();
        await Assert.That(activity.CreatedAtUnixSeconds >= 0).IsTrue();
        await Assert.That(activity.LikeCount >= 0).IsTrue();
        await Assert.That(activity.ViewCount >= 0).IsTrue();
    }

    public static async Task AssertValidUserPageInfo(UserPageInfo? pageInfo)
    {
        await Assert.That(pageInfo).IsNotNull();
        await Assert.That(pageInfo!.Cursor).IsNotNull();
        await Assert.That(pageInfo.NextOffset >= 0).IsTrue();
        await Assert.That(pageInfo.Total >= 0).IsTrue();
    }
}

/// <summary>
/// 共享剧集测试场景模型。
/// </summary>
public sealed record TestDramaScenario(
    string SeriesId,
    Episode Episode,
    DramaCredit Credit,
    string UserId
);
