namespace PhoenixReadApi.Tests;

/// <summary>
/// PhoenixReadApiClient 的端到端测试。
///
/// 这些测试使用样例中的公开搜索词发现一部当前可用的剧集，然后复用该剧集的
/// 详情、分集和署名账号来覆盖客户端的各个公开请求入口。
/// </summary>
public class Tests
{
    private const string Keyword = "请君入我怀";
    private const string FallbackUserId = "3112051666323866";

    private static readonly PhoenixReadApiClient Client = new(
        new PhoenixReadApiOptions
        {
            MaxPagesPerCategory = 1,
            RequestTimeout = TimeSpan.FromSeconds(25),
            RetryCount = 2,
            CompleteSearchSeasons = false,
        }
    );

    private static readonly Lazy<Task<Scenario>> ScenarioTask = new(
        CreateScenarioAsync,
        LazyThreadSafetyMode.ExecutionAndPublication
    );

    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestGetCatalogAsync()
    {
        using CancellationTokenSource timeout = CreateTimeout();
        IReadOnlyList<Drama> catalog = await Client.GetCatalogAsync(
            PhoenixReadApiCategory.AiDrama,
            timeout.Token
        );

        await Assert.That(catalog.Count > 0).IsTrue();
        await Assert.That(catalog.All(IsValidDrama)).IsTrue();
    }

    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestGetDramaDetailsAsync()
    {
        using CancellationTokenSource timeout = CreateTimeout();
        Scenario scenario = await GetScenarioAsync(timeout.Token);
        DramaDetails details = await Client.GetDramaDetailsAsync(scenario.SeriesId, timeout.Token);

        await Assert.That(details.Drama.Id).IsEqualTo(scenario.SeriesId);
        await Assert.That(details.Episodes.Count > 0).IsTrue();
        await Assert
            .That(
                details
                    .Episodes.Select(static episode => episode.Number)
                    .SequenceEqual(Enumerable.Range(1, details.Episodes.Count))
            )
            .IsTrue();
        await Assert.That(details.Episodes.All(IsValidEpisode)).IsTrue();
    }

    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestResolveMediaAsync()
    {
        using CancellationTokenSource timeout = CreateTimeout();
        Scenario scenario = await GetScenarioAsync(timeout.Token);

        PlaybackMedia byIds = await Client.ResolveMediaAsync(
            scenario.SeriesId,
            scenario.Episode.VideoId,
            timeout.Token
        );

        await Assert.That(IsHttpUrl(byIds.Url)).IsTrue();
        await Assert.That(byIds.Variants.Count > 0).IsTrue();
    }

    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestSearchAsync()
    {
        using CancellationTokenSource timeout = CreateTimeout();
        Scenario scenario = await GetScenarioAsync(timeout.Token);
        SearchResult search = await Client.SearchAsync(Keyword, timeout.Token);

        await Assert.That(search.Items.Count > 0).IsTrue();
        await Assert.That(search.Total >= search.Items.Count).IsTrue();
        await Assert.That(search.Items.All(IsValidDrama)).IsTrue();
        await Assert.That(search.Items.Any(item => item.Id == scenario.SeriesId)).IsTrue();
    }

    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestGetSuggestionsAsync()
    {
        using CancellationTokenSource timeout = CreateTimeout();
        IReadOnlyList<SearchSuggestion> suggestions = await Client.GetSuggestionsAsync(
            Keyword,
            maxResults: 5,
            cancellationToken: timeout.Token
        );
        IReadOnlyList<SearchSuggestion> clampedSuggestions = await Client.GetSuggestionsAsync(
            Keyword,
            maxResults: 25,
            cancellationToken: timeout.Token
        );

        await Assert.That(suggestions.Count > 0).IsTrue();
        await Assert.That(suggestions.Count <= 5).IsTrue();
        await Assert
            .That(suggestions.All(static item => !string.IsNullOrWhiteSpace(item.Name)))
            .IsTrue();
        await Assert.That(clampedSuggestions.Count <= 20).IsTrue();
    }

    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestGetRecommendationsAsync()
    {
        using CancellationTokenSource timeout = CreateTimeout();
        RecommendationPage queryPage = await Client.GetRecommendationsAsync(
            new RecommendationQuery
            {
                Genre = RecommendationGenre.ShortDrama,
                Offset = 0,
                SessionId = string.Empty,
                Seen = [],
            },
            timeout.Token
        );
        RecommendationPage overloadPage = await Client.GetRecommendationsAsync(
            RecommendationGenre.ShortDrama,
            offset: 0,
            sessionId: queryPage.SessionId,
            seen: queryPage.Items.Select(static item => item.Id),
            cancellationToken: timeout.Token
        );

        await Assert.That(queryPage.Items.Count > 0).IsTrue();
        await Assert.That(queryPage.Items.All(IsValidDrama)).IsTrue();
        await Assert.That(overloadPage.Items.All(IsValidDrama)).IsTrue();
        await Assert.That(overloadPage.NextOffset >= 0).IsTrue();
    }

    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestGetDanmakuAsync()
    {
        using CancellationTokenSource timeout = CreateTimeout();
        Scenario scenario = await GetScenarioAsync(timeout.Token);
        DanmakuPage danmaku = await Client.GetDanmakuAsync(
            scenario.SeriesId,
            scenario.Episode.VideoId,
            startMs: 0,
            durationMs: 60_000,
            timeout.Token
        );

        await Assert.That(danmaku.EpisodeId).IsEqualTo(scenario.Episode.VideoId);
        await Assert.That(danmaku.StartMs).IsEqualTo(0L);
        await Assert.That(danmaku.NextMs >= danmaku.StartMs).IsTrue();
        await Assert
            .That(
                danmaku.Items.All(item =>
                    item.TimeMs >= danmaku.StartMs && item.TimeMs < danmaku.NextMs
                )
            )
            .IsTrue();
    }

    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestGetDramaReviewsAsync()
    {
        using CancellationTokenSource timeout = CreateTimeout();
        Scenario scenario = await GetScenarioAsync(timeout.Token);
        CommentPage reviews = await Client.GetDramaReviewsAsync(
            scenario.SeriesId,
            count: 5,
            cancellationToken: timeout.Token
        );

        await Assert.That(reviews.TargetId).IsEqualTo(scenario.SeriesId);
        await Assert.That(reviews.Items.Count <= 5).IsTrue();
        await Assert.That(reviews.Total >= reviews.Items.Count).IsTrue();
        await Assert.That(reviews.Items.All(IsValidComment)).IsTrue();

        if (reviews.HasMore && reviews.Cursor.Length > 0)
        {
            CommentPage nextPage = await Client.GetDramaReviewsAsync(
                scenario.SeriesId,
                count: 5,
                cursor: reviews.Cursor,
                cancellationToken: timeout.Token
            );
            await Assert.That(nextPage.TargetId).IsEqualTo(scenario.SeriesId);
        }
    }

    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestGetEpisodeCommentsAsync()
    {
        using CancellationTokenSource timeout = CreateTimeout();
        Scenario scenario = await GetScenarioAsync(timeout.Token);
        CommentPage byIds = await Client.GetEpisodeCommentsAsync(
            scenario.SeriesId,
            scenario.Episode.VideoId,
            count: 5,
            cancellationToken: timeout.Token
        );

        await Assert.That(byIds.TargetId).IsEqualTo(scenario.Episode.VideoId);
        await Assert.That(byIds.Items.Count <= 5).IsTrue();
        await Assert.That(byIds.Total >= byIds.Items.Count).IsTrue();
        await Assert.That(byIds.Items.All(IsValidComment)).IsTrue();

        if (byIds.HasMore && byIds.Cursor.Length > 0)
        {
            CommentPage nextPage = await Client.GetEpisodeCommentsAsync(
                scenario.SeriesId,
                scenario.Episode.VideoId,
                count: 5,
                cursor: byIds.Cursor,
                cancellationToken: timeout.Token
            );
            await Assert.That(nextPage.TargetId).IsEqualTo(scenario.Episode.VideoId);
        }
    }

    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestGetUserProfileAsync()
    {
        using CancellationTokenSource timeout = CreateTimeout();
        Scenario scenario = await GetScenarioAsync(timeout.Token);

        UserProfile byCredit = await Client.GetUserProfileAsync(scenario.Credit, timeout.Token);
        UserProfile byId = await Client.GetUserProfileAsync(scenario.UserId, timeout.Token);
        UserDramaPage related = await Client.GetUserRelatedDramasAsync(
            scenario.UserId,
            count: 18,
            cancellationToken: timeout.Token
        );
        UserActivityPage activities = await Client.GetUserActivitiesAsync(
            scenario.UserId,
            count: 18,
            cancellationToken: timeout.Token
        );
        UserDramaPage collections = await Client.GetUserCollectionsAsync(
            scenario.UserId,
            count: 18,
            offset: 0,
            cancellationToken: timeout.Token
        );
        UserDramaPage likes = await Client.GetUserLikesAsync(
            scenario.UserId,
            count: 18,
            offset: 0,
            cancellationToken: timeout.Token
        );

        await Assert.That(byCredit.User.Id).IsEqualTo(scenario.UserId);
        await Assert.That(byId.User.Id).IsEqualTo(scenario.UserId);
        await Assert.That(IsValidPage(related.Items, related.Total)).IsTrue();
        await Assert.That(IsValidPage(activities.Items, activities.Total)).IsTrue();
        await Assert.That(IsValidPage(collections.Items, collections.Total)).IsTrue();
        await Assert.That(IsValidPage(likes.Items, likes.Total)).IsTrue();

        if (related.HasMore && related.Cursor.Length > 0)
        {
            UserDramaPage nextRelated = await Client.GetUserRelatedDramasAsync(
                scenario.UserId,
                count: 18,
                cursor: related.Cursor,
                cancellationToken: timeout.Token
            );
            await Assert.That(IsValidPage(nextRelated.Items, nextRelated.Total)).IsTrue();
        }

        if (activities.HasMore && activities.Cursor.Length > 0)
        {
            UserActivityPage nextActivities = await Client.GetUserActivitiesAsync(
                scenario.UserId,
                count: 18,
                cursor: activities.Cursor,
                cancellationToken: timeout.Token
            );
            await Assert.That(IsValidPage(nextActivities.Items, nextActivities.Total)).IsTrue();
        }

        if (collections.HasMore && collections.NextOffset > 0)
        {
            UserDramaPage nextCollections = await Client.GetUserCollectionsAsync(
                scenario.UserId,
                count: 18,
                offset: collections.NextOffset,
                cancellationToken: timeout.Token
            );
            await Assert.That(IsValidPage(nextCollections.Items, nextCollections.Total)).IsTrue();
        }

        if (likes.HasMore && likes.NextOffset > 0)
        {
            UserDramaPage nextLikes = await Client.GetUserLikesAsync(
                scenario.UserId,
                count: 18,
                offset: likes.NextOffset,
                cancellationToken: timeout.Token
            );
            await Assert.That(IsValidPage(nextLikes.Items, nextLikes.Total)).IsTrue();
        }
    }

    [Test]
    public Task TestDispose()
    {
        using HttpClient httpClient = new();
        using PhoenixReadApiClient client = new(
            httpClient,
            new PhoenixReadApiOptions { RequestTimeout = TimeSpan.FromSeconds(5) }
        );

        client.Dispose();
        client.Dispose();
        return Task.CompletedTask;
    }

    private static CancellationTokenSource CreateTimeout() => new(TimeSpan.FromMinutes(2));

    private static async Task<Scenario> GetScenarioAsync(CancellationToken cancellationToken) =>
        await ScenarioTask.Value.WaitAsync(cancellationToken);

    private static async Task<Scenario> CreateScenarioAsync()
    {
        using CancellationTokenSource timeout = new(TimeSpan.FromMinutes(4));
        SearchResult search = await Client.SearchAsync(Keyword, timeout.Token);
        Drama? selected = search.Items.FirstOrDefault(item =>
            string.Equals(item.Title, Keyword, StringComparison.OrdinalIgnoreCase)
        );
        selected ??= search.Items.FirstOrDefault(item =>
            item.Title.Contains(Keyword, StringComparison.OrdinalIgnoreCase)
        );
        selected ??= search.Items.FirstOrDefault(IsValidDrama);

        if (selected is null)
        {
            IReadOnlyList<Drama> catalog = await Client.GetCatalogAsync(
                PhoenixReadApiCategory.AiDrama,
                timeout.Token
            );
            selected = catalog.FirstOrDefault(IsValidDrama);
        }

        Drama selectedDrama =
            selected ?? throw new InvalidOperationException("搜索和剧库都没有返回可用剧集。");
        DramaDetails details = await Client.GetDramaDetailsAsync(selectedDrama.Id, timeout.Token);
        Episode episode =
            details.Episodes.FirstOrDefault(IsValidEpisode)
            ?? throw new InvalidOperationException("剧集详情没有返回可用分集。");

        DramaCredit? credit = details.Drama.Credits.FirstOrDefault(item =>
            IsNumericId(item.User.Id)
        );
        string userId = credit?.User.Id ?? FallbackUserId;
        credit ??= new DramaCredit { User = new User { Id = userId } };

        return new Scenario(details.Drama.Id, episode, credit, userId);
    }

    private static bool IsValidDrama(Drama drama) =>
        IsNumericId(drama.Id) && !string.IsNullOrWhiteSpace(drama.Title);

    private static bool IsValidEpisode(Episode episode) =>
        episode.Number > 0 && IsNumericId(episode.VideoId);

    private static bool IsValidComment(DramaComment comment) =>
        !string.IsNullOrWhiteSpace(comment.Id) && !string.IsNullOrWhiteSpace(comment.Text);

    private static bool IsValidPage<T>(IReadOnlyList<T> items, long total) =>
        items.Count <= 50 && total >= 0;

    private static bool IsNumericId(string value) =>
        value.Length > 0 && value.All(static character => character is >= '0' and <= '9');

    private static bool IsHttpUrl(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out Uri? uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    private sealed record Scenario(
        string SeriesId,
        Episode Episode,
        DramaCredit Credit,
        string UserId
    );
}
