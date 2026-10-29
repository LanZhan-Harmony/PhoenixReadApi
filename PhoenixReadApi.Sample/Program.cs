using System.Diagnostics;
using PhoenixReadApi;

const string keyword = "请君入我怀";
string sampleDirectory = GetSampleDirectory();
string sessionPath = Path.Combine(sampleDirectory, ".auth", "phone-session.json");
string phone = ReadPhone(sampleDirectory);
Directory.CreateDirectory(Path.GetDirectoryName(sessionPath)!);

using CancellationTokenSource timeout = new(TimeSpan.FromMinutes(15));
using PhoenixReadApiClient client = new(
    new PhoenixReadApiOptions
    {
        MaxPagesPerCategory = 1,
        RequestTimeout = TimeSpan.FromSeconds(25),
        RetryCount = 1,
        CompleteSearchSeasons = false,
        AuthenticationStateFilePath = sessionPath,
    }
);

Console.WriteLine($"关键词：{keyword}");
Console.WriteLine($"会话文件：{sessionPath}");

AuthenticationState authentication = client.GetAuthenticationState();
Console.WriteLine($"已有登录会话：{authentication.IsAuthenticated}");

bool isSessionValid = await client.ValidateSessionAsync(timeout.Token);
Console.WriteLine($"会话服务端校验有效性：{isSessionValid}");

if (isSessionValid)
{
    CurrentUserInfo? currentUser = await TryAsync(
        "GetCurrentUserInfoAsync",
        () => client.GetCurrentUserInfoAsync(timeout.Token)
    );
    if (currentUser is not null)
    {
        Console.WriteLine(
            $"  当前登录用户：{currentUser.Name} (ID: {currentUser.Id}), VIP: {currentUser.IsVip}"
        );
    }
}

// 演示二维码登录接口
QrCodeLoginSession? qrSession = await TryAsync(
    "CreateQrCodeLoginAsync",
    () => client.CreateQrCodeLoginAsync(timeout.Token)
);
if (qrSession is not null)
{
    Console.WriteLine(
        $"  获取到扫码 Token: {qrSession.Token[..Math.Min(16, qrSession.Token.Length)]}..., 有效秒数: {qrSession.ExpiresInSeconds}"
    );
    QrCodeCheckResult? qrStatus = await TryAsync(
        "CheckQrCodeStatusAsync",
        () => client.CheckQrCodeStatusAsync(qrSession.Token, timeout.Token)
    );
    if (qrStatus is not null)
    {
        Console.WriteLine($"  当前扫码状态：{qrStatus.Status}");
    }
}

// 批量详情
IReadOnlyList<Drama>? batchDramas = await TryAsync(
    "GetBatchDramaDetailsAsync",
    () =>
        client.GetBatchDramaDetailsAsync(
            ["7683879553894190105", "7690614743660235800"],
            timeout.Token
        )
);
PrintCount("  批量短剧详情", batchDramas?.Count);
if (batchDramas is not null)
{
    foreach (Drama drama in batchDramas)
    {
        Console.WriteLine(
            $"    [{drama.Id}] {drama.Title} - {drama.EpisodeCount}集 - 评分: {drama.Score}"
        );
    }
}

// 题材标签树
IReadOnlyList<TagCategoryGroup>? tagGroups = await TryAsync(
    "GetTagCategoriesAsync",
    () => client.GetTagCategoriesAsync(timeout.Token)
);
PrintCount("  题材分类分组", tagGroups?.Count);
if (tagGroups is { Count: > 0 })
{
    Console.WriteLine(
        $"    首个分组: {tagGroups[0].Name}, 包含标签数: {tagGroups[0].Items.Count} (例如: {tagGroups[0].Items[0].Name}={tagGroups[0].Items[0].Id})"
    );
}

// 题材标签落地列表
TagDramaPage? tagPage = await TryAsync(
    "GetTagDramaPageAsync",
    () =>
        client.GetTagDramaPageAsync(
            "cate_261",
            offset: 0,
            count: 6,
            cancellationToken: timeout.Token
        )
);
PrintCount("  题材标签短剧页", tagPage?.Items.Count);

if (!authentication.IsAuthenticated && phone.Length > 0)
{
    VerificationCodeResult? sent = await TryAsync(
        "SendVerificationCodeAsync",
        () => client.SendVerificationCodeAsync(phone, timeout.Token)
    );
    if (sent is not null)
    {
        string code =
            Environment.GetEnvironmentVariable("PHOENIX_READ_API_SMS_CODE") ?? string.Empty;
        if (code.Length == 0)
        {
            Console.Write("请输入短信验证码（也可以设置 PHOENIX_READ_API_SMS_CODE）：");
            code = Console.ReadLine()?.Trim() ?? string.Empty;
        }

        if (code.Length > 0)
        {
            AuthenticatedUser? user = await TryAsync(
                "LoginAsync",
                () => client.LoginAsync(phone, code, timeout.Token)
            );
            if (user is not null)
            {
                Console.WriteLine($"  登录账号：{user.Id} | {user.Name}");
            }
        }
        else
        {
            Console.WriteLine("  未提供验证码，继续执行匿名和只读接口。");
        }
    }
}

authentication = client.GetAuthenticationState();
Console.WriteLine($"当前登录状态：{authentication.IsAuthenticated}");

IReadOnlyList<Drama>? catalog = await TryAsync(
    "GetCatalogAsync",
    () => client.GetCatalogAsync(PhoenixReadApiCategory.All, timeout.Token)
);
PrintCount("  剧库", catalog?.Count);

IReadOnlyList<SearchSuggestion>? suggestions = await TryAsync(
    "GetSuggestionsAsync",
    () => client.GetSuggestionsAsync(keyword, 5, timeout.Token)
);
PrintCount("  联想", suggestions?.Count);

SearchResult? search = await TryAsync(
    "SearchAsync",
    () => client.SearchAsync(keyword, timeout.Token)
);
if (search is not null)
{
    Console.WriteLine(
        $"  搜索结果：{search.Items.Count}/{search.Total}, Warning='{search.Warning}'"
    );
}

RecommendationPage? recommendationByQuery = await TryAsync(
    "GetRecommendationsAsync(query)",
    () =>
        client.GetRecommendationsAsync(
            new RecommendationQuery { Genre = RecommendationGenre.ShortDrama },
            timeout.Token
        )
);
PrintCount("  推荐(query)", recommendationByQuery?.Items.Count);

RecommendationPage? recommendationByOverload = await TryAsync(
    "GetRecommendationsAsync(overload)",
    () =>
        client.GetRecommendationsAsync(
            RecommendationGenre.ShortDrama,
            cancellationToken: timeout.Token
        )
);
PrintCount("  推荐(overload)", recommendationByOverload?.Items.Count);

CategoryQuery categoryQuery = new() { Category = PhoenixReadApiCategory.All };
CategoryPage? category = await TryAsync(
    "GetCategoryAsync",
    () => client.GetCategoryAsync(categoryQuery, timeout.Token)
);
PrintCount("  分类", category?.Items.Count);

CategoryPage? categorySelector = await TryAsync(
    "GetCategoryAsync",
    () => client.GetCategoryAsync(categoryQuery, timeout.Token)
);
PrintCount("  分类筛选器", categorySelector?.Options.Count);

FeedPage? newSeries = await TryAsync(
    "GetNewSeriesAsync",
    () => client.GetNewSeriesAsync(cancellationToken: timeout.Token)
);
PrintCount("  新剧分组", newSeries?.Sections.Count);

FeedPage? feedCategories = await TryAsync(
    "GetFeedCategoriesAsync",
    () => client.GetFeedCategoriesAsync(timeout.Token)
);
PrintCount("  首页分组", feedCategories?.Sections.Count);

BookPackFields? bookPackFields = await TryAsync(
    "GetBookPackFieldsAsync",
    () => client.GetBookPackFieldsAsync(cancellationToken: timeout.Token)
);
PrintCount("  书架字段", bookPackFields?.Values.Count);

Drama? selectedDrama = FindDrama(search, catalog, keyword);
if (selectedDrama is null || selectedDrama.Id.Length == 0)
{
    Console.WriteLine("未找到可继续测试详情、播放和评论接口的剧集。");
}

DramaDetails? details = selectedDrama is null
    ? null
    : await TryAsync(
        "GetDramaDetailsAsync",
        () => client.GetDramaDetailsAsync(selectedDrama.Id, timeout.Token)
    );

string seriesId = details?.Drama.Id ?? selectedDrama?.Id ?? string.Empty;
Episode? firstEpisode = details?.Episodes.FirstOrDefault(episode => episode.VideoId.Length > 0);
string videoId = firstEpisode?.VideoId ?? string.Empty;
Console.WriteLine($"剧集：{seriesId}，分集：{details?.Episodes.Count ?? 0}，首集：{videoId}");

PlaybackMedia? mediaByIds = await TryWhenIdsAsync(
    "ResolveMediaAsync(ids)",
    seriesId,
    videoId,
    (series, video) => client.ResolveMediaAsync(series, video, timeout.Token)
);
PrintMedia(mediaByIds);

long durationMs =
    mediaByIds?.Duration > TimeSpan.Zero
        ? (long)Math.Ceiling(mediaByIds.Duration.TotalMilliseconds)
        : 600_000L;
DanmakuPage? danmaku = await TryWhenIdsAsync(
    "GetDanmakuAsync",
    seriesId,
    videoId,
    (series, video) => client.GetDanmakuAsync(series, video, 0, durationMs, timeout.Token)
);
if (danmaku is not null)
{
    Console.WriteLine($"  弹幕：{danmaku.Items.Count}/{danmaku.Total}，NextMs={danmaku.NextMs}");
}

CommentPage? reviews = await TryWhenIdAsync(
    "GetDramaReviewsAsync",
    seriesId,
    id => client.GetDramaReviewsAsync(id, 10, cancellationToken: timeout.Token)
);
PrintComments("  剧评", reviews);

CommentPage? episodeComments = await TryWhenIdsAsync(
    "GetEpisodeCommentsAsync(ids)",
    seriesId,
    videoId,
    (series, video) =>
        client.GetEpisodeCommentsAsync(series, video, 10, cancellationToken: timeout.Token)
);
PrintComments("  分集评论", episodeComments);

DramaComment? sampleComment =
    reviews?.Items.FirstOrDefault() ?? episodeComments?.Items.FirstOrDefault();
CommentReplyPage? replies = sampleComment is null
    ? null
    : await TryAsync(
        "GetCommentRepliesAsync",
        () => client.GetCommentRepliesAsync(sampleComment.Id, cancellationToken: timeout.Token)
    );
PrintCount("  评论回复", replies?.Items.Count);

CommentReplyPage? reviewReplies = sampleComment is null
    ? null
    : await TryAsync(
        "GetCommentRepliesAsync",
        () => client.GetCommentRepliesAsync(sampleComment.Id, cancellationToken: timeout.Token)
    );
PrintCount("  剧评回复", reviewReplies?.Items.Count);

string creditUserId = details?.Drama.Credits.FirstOrDefault()?.User.Id ?? string.Empty;
UserProfile? creditProfile = await TryWhenIdAsync(
    "GetUserProfileAsync(userId)",
    creditUserId,
    id => client.GetUserProfileAsync(id, timeout.Token)
);
PrintProfile(creditProfile);

if (details?.Drama.Credits.FirstOrDefault() is { } credit)
{
    UserProfile? creditProfileByCredit = await TryAsync(
        "GetUserProfileAsync(credit)",
        () => client.GetUserProfileAsync(credit, timeout.Token)
    );
    PrintProfile(creditProfileByCredit);
}

string ordinaryUserId = FindOrdinaryUserId(episodeComments, details);
string profileUserId = ordinaryUserId.Length > 0 ? ordinaryUserId : creditUserId;
UserActivityPage? activities = await TryWhenIdAsync(
    "GetUserActivitiesAsync",
    profileUserId,
    id => client.GetUserActivitiesAsync(id, cancellationToken: timeout.Token)
);
PrintCount("  用户动态", activities?.Items.Count);

if (activities is { HasMore: true, Cursor.Length: > 0 })
{
    UserActivityPage? nextActivities = await TryAsync(
        "GetUserActivitiesAsync(next)",
        () =>
            client.GetUserActivitiesAsync(
                profileUserId,
                cursor: activities.Cursor,
                cancellationToken: timeout.Token
            )
    );
    PrintCount("  用户动态下一页", nextActivities?.Items.Count);
}

UserDramaPage? userRelated = await TryWhenIdAsync(
    "GetUserRelatedDramasAsync",
    profileUserId,
    id => client.GetUserRelatedDramasAsync(id, cancellationToken: timeout.Token)
);
PrintCount("  用户关联剧集", userRelated?.Items.Count);

UserDramaPage? userCollections = await TryWhenIdAsync(
    "GetUserCollectionsAsync",
    profileUserId,
    id => client.GetUserCollectionsAsync(id, cancellationToken: timeout.Token)
);
PrintCount("  用户收藏", userCollections?.Items.Count);

UserDramaPage? userLikes = await TryWhenIdAsync(
    "GetUserLikesAsync",
    profileUserId,
    id => client.GetUserLikesAsync(id, cancellationToken: timeout.Token)
);
PrintCount("  用户点赞", userLikes?.Items.Count);

ReadHistoryPage? history = await TryAsync(
    "GetHistoryAsync",
    () => client.GetHistoryAsync(cancellationToken: timeout.Token)
);
PrintCount("  历史", history?.Items.Count);

ReadProgressPage? progress = await TryAsync(
    "GetReadProgressAsync",
    () => client.GetReadProgressAsync(timeout.Token)
);
PrintCount("  播放进度", progress?.Items.Count);

IReadOnlyList<string> progressIds =
[
    .. new[] { seriesId, videoId }
        .Where(static id => id.Length > 0)
        .Distinct(StringComparer.Ordinal),
];
ReadProgressPage? progressFor =
    progressIds.Count == 0
        ? null
        : await TryAsync(
            "GetReadProgressForAsync",
            () => client.GetReadProgressForAsync(progressIds, timeout.Token)
        );
PrintCount("  指定播放进度", progressFor?.Items.Count);

DramaPage? favorites = await TryAsync(
    "GetFavoritesAsync",
    () => client.GetFavoritesAsync(cancellationToken: timeout.Token)
);
PrintCount("  收藏", favorites?.Items.Count);

(IReadOnlyList<string> Ids, DramaPage Page)? favoriteIds = await TryFavoriteIdsAsync(
    "GetFavoriteIdsAsync",
    () => client.GetFavoriteIdsAsync(cancellationToken: timeout.Token)
);
PrintCount("  收藏 ID", favoriteIds?.Ids.Count);

DramaPage? likes = await TryAsync(
    "GetLikesAsync",
    () => client.GetLikesAsync(cancellationToken: timeout.Token)
);
PrintCount("  点赞剧集", likes?.Items.Count);

FollowUpdatePage? followUpdates = await TryAsync(
    "GetFollowUpdatesAsync",
    () => client.GetFollowUpdatesAsync(cancellationToken: timeout.Token)
);
PrintCount("  关注动态", followUpdates?.Items.Count);

FollowUpdatePage? allFollowUpdates = await TryAsync(
    "GetFollowUpdatesAsync",
    () => client.GetFollowUpdatesAsync(cancellationToken: timeout.Token)
);
PrintCount("  全部关注动态", allFollowUpdates?.Items.Count);

ReservationPage? reservations = await TryAsync(
    "GetReservationsAsync",
    () => client.GetReservationsAsync(cancellationToken: timeout.Token)
);
PrintCount("  预约", reservations?.Items.Count);

ReservationPage? calendar = await TryAsync(
    "GetCalendarAsync",
    () => client.GetCalendarAsync(cancellationToken: timeout.Token)
);
PrintCount("  日历", calendar?.Items.Count);

ReservationPage? upcomingReservations = await TryAsync(
    "GetUpcomingReservationsAsync",
    () => client.GetUpcomingReservationsAsync(cancellationToken: timeout.Token)
);
PrintCount("  可预约新剧", upcomingReservations?.Items.Count);

ShareLink? share = await TryWhenIdsAsync(
    "CreateShareLinkAsync",
    seriesId,
    videoId,
    (series, video) => client.CreateShareLinkAsync(series, video, timeout.Token)
);
if (share is not null)
{
    Console.WriteLine($"  分享：{share.Url}");
}

ShareLink? parsedShare = RunSync(
    "ResolveShareLink",
    () =>
        client.ResolveShareLink(
            share is { Url.Length: > 0 }
                ? share.Url
                : $"https://hongguoduanju.com/?series_id={seriesId}&video_id={videoId}"
        )
);
if (parsedShare is not null)
{
    Console.WriteLine($"  分享解析：{parsedShare.SeriesId}/{parsedShare.VideoId}");
}

// These calls exercise the public write methods as well. They are guarded so
// an anonymous run still completes every read-only part of the sample.
if (authentication.IsAuthenticated)
{
    HashSet<string> favoriteSeriesIds =
        favorites
            ?.Items.Select(static item => item.Id)
            .Where(static id => id.Length > 0)
            .ToHashSet(StringComparer.Ordinal)
        ?? [];
    HashSet<string> likedSeriesIds =
        likes
            ?.Items.Select(static item => item.Id)
            .Where(static id => id.Length > 0)
            .ToHashSet(StringComparer.Ordinal)
        ?? [];
    ReadHistoryEntry? existingHistory = history?.Items.FirstOrDefault(item =>
        item.SeriesId == seriesId && item.VideoId == videoId
    );
    HashSet<string> existingReviewIds =
        reviews?.Items.Select(static item => item.Id).ToHashSet(StringComparer.Ordinal) ?? [];
    HashSet<string> existingEpisodeCommentIds =
        episodeComments?.Items.Select(static item => item.Id).ToHashSet(StringComparer.Ordinal)
        ?? [];
    HashSet<string> existingDanmakuIds =
        danmaku?.Items.Select(static item => item.Id).ToHashSet(StringComparer.Ordinal) ?? [];
    HashSet<string> existingReplyIds =
        replies?.Items.Select(static item => item.Id).ToHashSet(StringComparer.Ordinal) ?? [];
    await RunOperationsAsync(
        client,
        seriesId,
        videoId,
        firstEpisode,
        sampleComment,
        creditUserId,
        favoriteSeriesIds,
        likedSeriesIds,
        existingHistory,
        existingReviewIds,
        existingEpisodeCommentIds,
        existingDanmakuIds,
        existingReplyIds,
        timeout.Token
    );
}
else
{
    Console.WriteLine("未登录，跳过需要账号权限的写入接口。");
}

Console.WriteLine("样例执行完成。会话文件按要求保留，未调用 Logout()。");

static string GetSampleDirectory()
{
    string candidate = Path.Combine(Environment.CurrentDirectory, "PhoenixReadApi.Sample");
    return Directory.Exists(candidate) ? candidate : AppContext.BaseDirectory;
}

static string ReadPhone(string sampleDirectory)
{
    string path = Path.Combine(sampleDirectory, ".auth", "Phone.txt");
    return File.Exists(path) ? File.ReadAllText(path).Trim() : string.Empty;
}

static Drama? FindDrama(SearchResult? search, IReadOnlyList<Drama>? catalog, string keyword)
{
    return search?.Items.FirstOrDefault(item =>
            string.Equals(item.Title, keyword, StringComparison.OrdinalIgnoreCase)
        )
        ?? search?.Items.FirstOrDefault(item =>
            item.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase)
        )
        ?? search?.Items.FirstOrDefault()
        ?? catalog?.FirstOrDefault(item =>
            item.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase)
        )
        ?? catalog?.FirstOrDefault();
}

static string FindOrdinaryUserId(CommentPage? comments, DramaDetails? details)
{
    HashSet<string> creditIds =
        details
            ?.Drama.Credits.Select(static credit => credit.User.Id)
            .Where(static id => id.Length > 0)
            .ToHashSet(StringComparer.Ordinal)
        ?? [];
    return comments
            ?.Items.Select(static comment => comment.Author.Id)
            .FirstOrDefault(id => id.Length > 0 && !creditIds.Contains(id))
        ?? string.Empty;
}

static void PrintMedia(PlaybackMedia? media)
{
    if (media is null)
    {
        return;
    }
    Console.WriteLine(
        $"  播放：{media.Url} | {media.Quality}p | variants={media.Variants.Count} | key={(media.DecryptionKey.Length > 0 ? "yes" : "no")}"
    );
}

static void PrintComments(string label, CommentPage? page)
{
    if (page is not null)
    {
        Console.WriteLine($"{label}：{page.Items.Count}/{page.Total}，HasMore={page.HasMore}");
    }
}

static void PrintProfile(UserProfile? profile)
{
    if (profile is not null)
    {
        Console.WriteLine(
            $"  用户：{profile.User.Id}/{profile.User.Name} | collections={profile.Collections.Count} likes={profile.Likes.Count} activities={profile.Activities.Count} related={profile.RelatedDramas.Count}"
        );
    }
}

static void PrintCount(string label, int? count)
{
    if (count is not null)
    {
        Console.WriteLine($"{label}：{count}");
    }
}

static async Task<T?> TryAsync<T>(string name, Func<Task<T>> operation)
    where T : class
{
    Stopwatch stopwatch = Stopwatch.StartNew();
    Console.WriteLine($"[RUN] {name}");
    try
    {
        T result = await operation();
        Console.WriteLine($"[ OK] {name} ({stopwatch.ElapsedMilliseconds} ms)");
        return result;
    }
    catch (PhoenixReadApiException exception)
    {
        Console.WriteLine($"[ERR] {name}: {exception.Message} | {exception.Endpoint ?? "unknown"}");
    }
    catch (Exception exception)
    {
        Console.WriteLine($"[ERR] {name}: {exception.GetType().Name}: {exception.Message}");
    }
    return null;
}

static async Task<T?> TryWhenIdAsync<T>(string name, string id, Func<string, Task<T>> operation)
    where T : class
{
    return id.Length == 0 ? null : await TryAsync(name, () => operation(id));
}

static async Task<T?> TryWhenIdsAsync<T>(
    string name,
    string firstId,
    string secondId,
    Func<string, string, Task<T>> operation
)
    where T : class
{
    return firstId.Length == 0 || secondId.Length == 0
        ? null
        : await TryAsync(name, () => operation(firstId, secondId));
}

static async Task<(IReadOnlyList<string> Ids, DramaPage Page)?> TryFavoriteIdsAsync(
    string name,
    Func<Task<(IReadOnlyList<string> Ids, DramaPage Page)>> operation
)
{
    Stopwatch stopwatch = Stopwatch.StartNew();
    Console.WriteLine($"[RUN] {name}");
    try
    {
        (IReadOnlyList<string> Ids, DramaPage Page) result = await operation();
        Console.WriteLine($"[ OK] {name} ({stopwatch.ElapsedMilliseconds} ms)");
        return result;
    }
    catch (PhoenixReadApiException exception)
    {
        Console.WriteLine($"[ERR] {name}: {exception.Message} | {exception.Endpoint ?? "unknown"}");
    }
    catch (Exception exception)
    {
        Console.WriteLine($"[ERR] {name}: {exception.GetType().Name}: {exception.Message}");
    }
    return null;
}

static T? RunSync<T>(string name, Func<T> operation)
    where T : class
{
    Console.WriteLine($"[RUN] {name}");
    try
    {
        T result = operation();
        Console.WriteLine($"[ OK] {name}");
        return result;
    }
    catch (Exception exception)
    {
        Console.WriteLine($"[ERR] {name}: {exception.GetType().Name}: {exception.Message}");
        return default!;
    }
}

static async Task RunOperationsAsync(
    PhoenixReadApiClient client,
    string seriesId,
    string videoId,
    Episode? episode,
    DramaComment? comment,
    string userId,
    IReadOnlySet<string> favoriteSeriesIds,
    IReadOnlySet<string> likedSeriesIds,
    ReadHistoryEntry? existingHistory,
    IReadOnlySet<string> existingReviewIds,
    IReadOnlySet<string> existingEpisodeCommentIds,
    IReadOnlySet<string> existingDanmakuIds,
    IReadOnlySet<string> existingReplyIds,
    CancellationToken cancellationToken
)
{
    const string marker = "很不错";

    if (seriesId.Length > 0 && videoId.Length > 0)
    {
        await TryAsync(
            "ReportProgressAsync",
            () =>
                client.ReportProgressAsync(
                    new ProgressUpdate
                    {
                        SeriesId = seriesId,
                        VideoId = videoId,
                        EpisodeNumber = episode?.Number ?? 0,
                        CurrentPositionMs = 1_000,
                        DurationMs = 60_000,
                        AccumulatedPlayTimeMs = 1_000,
                    },
                    cancellationToken
                )
        );
        bool wasFavorite = favoriteSeriesIds.Contains(seriesId);
        await TryAsync(
            "SetFavoriteAsync",
            () => client.SetFavoriteAsync(seriesId, videoId, true, cancellationToken)
        );
        await TryAsync(
            "RemoveFavoriteAsync",
            () => client.RemoveFavoriteAsync(seriesId, videoId, cancellationToken)
        );
        if (wasFavorite)
        {
            await TryAsync(
                "SetFavoriteAsync(restore)",
                () => client.SetFavoriteAsync(seriesId, videoId, true, cancellationToken)
            );
        }

        bool wasLiked = likedSeriesIds.Contains(seriesId);
        await TryAsync(
            "SetDramaEpisodeLikeAsync",
            () => client.SetDramaEpisodeLikeAsync(seriesId, videoId, true, cancellationToken)
        );
        await TryAsync(
            "SetDramaEpisodeLikeAsync(false)",
            () => client.SetDramaEpisodeLikeAsync(seriesId, videoId, false, cancellationToken)
        );
        if (wasLiked)
        {
            await TryAsync(
                "SetDramaLikeAsync(restore)",
                () => client.SetDramaEpisodeLikeAsync(seriesId, videoId, true, cancellationToken)
            );
        }
        await TryAsync(
            "SendEpisodeCommentAsync",
            () => client.SendEpisodeCommentAsync(seriesId, videoId, marker, cancellationToken)
        );
        await TryAsync(
            "SendDanmakuAsync",
            () => client.SendDanmakuAsync(seriesId, videoId, marker, cancellationToken)
        );
        await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);

        CommentPage? createdComments = await TryAsync(
            "GetEpisodeCommentsAsync(cleanup)",
            () =>
                client.GetEpisodeCommentsAsync(
                    seriesId,
                    videoId,
                    20,
                    cancellationToken: cancellationToken
                )
        );
        if (createdComments is not null)
        {
            foreach (
                DramaComment created in createdComments.Items.Where(item =>
                    item.Text == marker && !existingEpisodeCommentIds.Contains(item.Id)
                )
            )
            {
                await TryAsync(
                    "DeleteCommentAsync(created)",
                    () => client.DeleteCommentAsync(created.Id, videoId, cancellationToken)
                );
            }
        }

        DanmakuPage? createdDanmaku = await TryAsync(
            "GetDanmakuAsync(cleanup)",
            () => client.GetDanmakuAsync(seriesId, videoId, 0, 60_000, cancellationToken)
        );
        if (createdDanmaku is not null)
        {
            foreach (
                DanmakuItem created in createdDanmaku.Items.Where(item =>
                    item.Text == marker && !existingDanmakuIds.Contains(item.Id)
                )
            )
            {
                await TryAsync(
                    "DeleteDanmakuAsync(created)",
                    () => client.DeleteDanmakuAsync(created.Id, videoId, cancellationToken)
                );
            }
        }
    }

    if (seriesId.Length > 0)
    {
        await TryAsync(
            "SendDramaReviewAsync",
            () => client.SendDramaReviewAsync(seriesId, 5.0, marker, cancellationToken)
        );
        await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        CommentPage? createdReviews = await TryAsync(
            "GetDramaReviewsAsync(cleanup)",
            () => client.GetDramaReviewsAsync(seriesId, 20, cancellationToken: cancellationToken)
        );
        if (createdReviews is not null)
        {
            foreach (
                DramaComment created in createdReviews.Items.Where(item =>
                    item.Text == marker && !existingReviewIds.Contains(item.Id)
                )
            )
            {
                await TryAsync(
                    "DeleteDramaReviewAsync(created)",
                    () => client.DeleteDramaReviewAsync(created.Id, seriesId, cancellationToken)
                );
            }
        }
        await TryAsync(
            "SetReservationAsync",
            () => client.SetReservationAsync(seriesId, true, cancellationToken)
        );
        await TryAsync(
            "DeleteReservationAsync",
            () => client.DeleteReservationAsync(seriesId, cancellationToken)
        );
    }

    if (comment is not null)
    {
        await TryAsync(
            "GetCommentRepliesAsync(write target)",
            () => client.GetCommentRepliesAsync(comment.Id, cancellationToken: cancellationToken)
        );
        await TryAsync(
            "SendCommentReplyAsync",
            () => client.SendCommentReplyAsync(comment.Id, videoId, marker, "", cancellationToken)
        );
        await TryAsync(
            "SendCommentReplyAsync",
            () => client.SendCommentReplyAsync(comment.Id, videoId, marker, "", cancellationToken)
        );
        await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        CommentReplyPage? createdReplies = await TryAsync(
            "GetCommentRepliesAsync(cleanup)",
            () =>
                client.GetCommentRepliesAsync(comment.Id, 20, cancellationToken: cancellationToken)
        );
        if (createdReplies is not null)
        {
            foreach (
                CommentReply created in createdReplies.Items.Where(item =>
                    item.Text == marker && !existingReplyIds.Contains(item.Id)
                )
            )
            {
                await TryAsync(
                    "SetReplyLikeAsync(created)",
                    () => client.SetReplyLikeAsync(created.Id, videoId, true, cancellationToken)
                );
                await TryAsync(
                    "SetReplyLikeAsync(created,false)",
                    () => client.SetReplyLikeAsync(created.Id, videoId, false, cancellationToken)
                );
                await TryAsync(
                    "DeleteReplyAsync(created)",
                    () => client.DeleteReplyAsync(created.Id, cancellationToken)
                );
            }
        }
        await TryAsync(
            "SetCommentLikeAsync",
            () => client.SetCommentLikeAsync(comment.Id, seriesId, true, cancellationToken)
        );
        await TryAsync(
            "SetCommentLikeAsync(false)",
            () => client.SetCommentLikeAsync(comment.Id, seriesId, false, cancellationToken)
        );
        await TryAsync(
            "SetReviewLikeAsync",
            () => client.SetReviewLikeAsync(comment.Id, seriesId, true, cancellationToken)
        );
        await TryAsync(
            "SetReviewLikeAsync(false)",
            () => client.SetReviewLikeAsync(comment.Id, seriesId, false, cancellationToken)
        );
        await TryAsync(
            "SetDanmakuLikeAsync",
            () => client.SetDanmakuLikeAsync(comment.Id, videoId, true, cancellationToken)
        );
        await TryAsync(
            "SetDanmakuLikeAsync(false)",
            () => client.SetDanmakuLikeAsync(comment.Id, videoId, false, cancellationToken)
        );
    }

    if (userId.Length > 0)
    {
        await TryAsync(
            "SetFollowAsync",
            () => client.SetFollowAsync(userId, true, cancellationToken)
        );
        await TryAsync(
            "SetFollowAsync(false)",
            () => client.SetFollowAsync(userId, false, cancellationToken)
        );
    }

    if (episode is not null && seriesId.Length > 0 && videoId.Length > 0)
    {
        await TryAsync(
            "DeleteHistoryAsync",
            () => client.DeleteHistoryAsync(seriesId, videoId, cancellationToken)
        );
        if (existingHistory is not null)
        {
            await TryAsync(
                "ReportProgressAsync(restore)",
                () =>
                    client.ReportProgressAsync(
                        new ProgressUpdate
                        {
                            SeriesId = existingHistory.SeriesId,
                            VideoId = existingHistory.VideoId,
                            EpisodeNumber = episode?.Number ?? 0,
                            CurrentPositionMs = existingHistory.CurrentPositionMs,
                            DurationMs = existingHistory.DurationMs,
                            AccumulatedPlayTimeMs = existingHistory.CurrentPositionMs,
                        },
                        cancellationToken
                    )
            );
        }
    }
}
