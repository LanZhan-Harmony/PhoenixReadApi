namespace PhoenixReadApi.Sample;

internal static partial class SampleSteps
{
    public static async Task LoginAsync(SampleContext context, string phone)
    {
        PhoenixReadApiClient client = context.Client;
        CancellationToken cancellationToken = context.CancellationToken;

        context.Authentication = client.GetAuthenticationState();
        Console.WriteLine($"已有登录会话：{context.Authentication.IsAuthenticated}");

        bool isSessionValid = await client.ValidateSessionAsync(cancellationToken);
        Console.WriteLine($"会话服务端校验有效性：{isSessionValid}");

        if (isSessionValid)
        {
            CurrentUserInfo? currentUser = await TryAsync(
                "GetCurrentUserInfoAsync",
                () => client.GetCurrentUserInfoAsync(cancellationToken)
            );
            if (currentUser is not null)
            {
                Console.WriteLine(
                    $"  当前登录用户：{currentUser.Name} (ID: {currentUser.Id}), VIP: {currentUser.IsVip}"
                );
            }
        }

        if (!context.Authentication.IsAuthenticated && phone.Length > 0)
        {
            VerificationCodeResult? sent = await TryAsync(
                "SendVerificationCodeAsync",
                () => client.SendVerificationCodeAsync(phone, cancellationToken)
            );
            if (sent is not null)
            {
                Console.Write("请输入短信验证码：");
                string code = Console.ReadLine()?.Trim() ?? string.Empty;

                if (code.Length > 0)
                {
                    AuthenticatedUser? user = await TryAsync(
                        "LoginAsync",
                        () => client.LoginAsync(phone, code, cancellationToken)
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

        context.Authentication = client.GetAuthenticationState();
        Console.WriteLine($"当前登录状态：{context.Authentication.IsAuthenticated}");

        QrCodeLoginSession? qrSession = await TryAsync(
            "CreateQrCodeLoginAsync",
            () => client.CreateQrCodeLoginAsync(cancellationToken)
        );
        if (qrSession is not null)
        {
            Console.WriteLine(
                $"  获取到扫码 Token: {qrSession.Token[..Math.Min(16, qrSession.Token.Length)]}..., 有效秒数: {qrSession.ExpiresInSeconds}"
            );
            QrCodeCheckResult? qrStatus = await TryAsync(
                "CheckQrCodeStatusAsync",
                () => client.CheckQrCodeStatusAsync(qrSession.Token, cancellationToken)
            );
            if (qrStatus is not null)
            {
                Console.WriteLine($"  当前扫码状态：{qrStatus.Status}");
            }
        }
    }

    public static async Task RunBasicReadOnlyAsync(SampleContext context)
    {
        PhoenixReadApiClient client = context.Client;
        CancellationToken cancellationToken = context.CancellationToken;

        context.Catalog = await TryAsync(
            "GetCatalogAsync",
            () => client.GetCatalogAsync(PhoenixReadApiCategory.All, cancellationToken)
        );
        PrintCount("  剧库", context.Catalog?.Count);

        IReadOnlyList<LoginRegion> loginRegions = client.GetLoginRegions();
        Console.WriteLine($"  登录支持区域: {loginRegions.Count} 个 (示例: {loginRegions[0].Name} +{loginRegions[0].Code})");

        DislikePanel? dislikePanel = await TryAsync(
            "GetDislikePanelAsync",
            () => client.GetDislikePanelAsync(2, cancellationToken)
        );
        PrintCount("  负反馈面板分组", dislikePanel?.Groups.Count);

        EmoticonPage? emoticons = await TryAsync(
            "GetEmoticonsAsync",
            () => client.GetEmoticonsAsync(isCollected: false, offset: 0, count: 5, cancellationToken: cancellationToken)
        );
        PrintCount("  热门表情包", emoticons?.Items.Count);

        IReadOnlyList<SearchSuggestion>? suggestions = await TryAsync(
            "GetSuggestionsAsync",
            () => client.GetSuggestionsAsync(context.Keyword, 5, cancellationToken)
        );
        PrintCount("  联想", suggestions?.Count);

        IReadOnlyList<string>? searchCues = await TryAsync(
            "GetSearchCuesAsync",
            () => client.GetSearchCuesAsync(cancellationToken)
        );
        PrintCount("  搜索轮播热词", searchCues?.Count);
        if (searchCues is { Count: > 0 })
        {
            Console.WriteLine($"    轮播热词预览: {string.Join(" / ", searchCues.Take(4))}");
        }

        HotSearch? hotSearch = await TryAsync(
            "GetHotSearchAsync",
            () => client.GetHotSearchAsync(cancellationToken)
        );
        if (hotSearch is not null)
        {
            Console.WriteLine(
                $"  热搜看板：猜你想搜 {hotSearch.Guesses.Count} 条，看板榜单 {hotSearch.Boards.Count} 个"
            );
            foreach (HotSearchBoard board in hotSearch.Boards.Take(2))
            {
                Console.WriteLine($"    ★ {board.Title}:");
                foreach (HotSearchItem item in board.Items.Take(3))
                {
                    Console.WriteLine($"       - {item.Title} [{item.CardTips}]");
                }
            }
        }

        context.Search = await TryAsync(
            "SearchAsync",
            () => client.SearchAsync(context.Keyword, cancellationToken)
        );
        if (context.Search is not null)
        {
            Console.WriteLine(
                $"  搜索结果：{context.Search.Dramas.Count}/{context.Search.Total}, Warning='{context.Search.Warning}'"
            );
        }

        // 批量详情：从搜索/剧库结果取前两个剧集 ID
        string[] batchIds =
        [
            .. (context.Search?.Dramas ?? context.Catalog ?? [])
                .Select(static item => item.Id)
                .Where(static id => id.Length > 0)
                .Distinct(StringComparer.Ordinal)
                .Take(2),
        ];
        if (batchIds.Length > 0)
        {
            IReadOnlyList<Drama>? batchDramas = await TryAsync(
                "GetBatchDramaDetailsAsync",
                () => client.GetBatchDramaDetailsAsync(batchIds, cancellationToken)
            );
            PrintCount("  批量短剧详情 (图书级)", batchDramas?.Count);

            IReadOnlyList<DramaDetails>? batchWithEpisodes = await TryAsync(
                "GetBatchDramaDetailsWithEpisodesAsync",
                () => client.GetBatchDramaDetailsWithEpisodesAsync(batchIds, cancellationToken)
            );
            PrintCount("  批量短剧详情 (播放器级含分集)", batchWithEpisodes?.Count);
            if (batchWithEpisodes is not null)
            {
                foreach (DramaDetails item in batchWithEpisodes)
                {
                    Console.WriteLine(
                        $"    [{item.Drama.Id}] 《{item.Drama.Title}》 - {item.Episodes.Count} 集 - 首集: {item.Episodes.FirstOrDefault()?.Title}"
                    );
                }
            }
        }

        RecommendationPage? shortDramaRecommendations = await TryAsync(
            "GetRecommendationsAsync",
            () =>
                client.GetRecommendationsAsync(
                    RecommendationGenre.ShortDrama,
                    cancellationToken: cancellationToken
                )
        );
        PrintCount("  真人短剧推荐", shortDramaRecommendations?.Items.Count);
        RecommendationPage? comicDramaRecommendations = await TryAsync(
            "GetRecommendationsAsync",
            () =>
                client.GetRecommendationsAsync(
                    RecommendationGenre.ComicDrama,
                    cancellationToken: cancellationToken
                )
        );
        PrintCount("  漫剧推荐", comicDramaRecommendations?.Items.Count);
        RecommendationPage? aiDramaRecommendations = await TryAsync(
            "GetRecommendationsAsync",
            () =>
                client.GetRecommendationsAsync(
                    RecommendationGenre.AiDrama,
                    cancellationToken: cancellationToken
                )
        );
        PrintCount("  AI短剧推荐", aiDramaRecommendations?.Items.Count);
    }

    public static async Task RunDramaDerivedAsync(SampleContext context)
    {
        PhoenixReadApiClient client = context.Client;
        CancellationToken cancellationToken = context.CancellationToken;

        Drama? selectedDrama = context.FindDrama();
        if (selectedDrama is null || selectedDrama.Id.Length == 0)
        {
            Console.WriteLine("未找到可继续测试详情、播放和评论接口的剧集。");
        }

        context.Details = selectedDrama is null
            ? null
            : await TryAsync(
                "GetDramaDetailsAsync",
                () => client.GetDramaDetailsAsync(selectedDrama.Id, cancellationToken)
            );

        context.SeriesId = context.Details?.Drama.Id ?? selectedDrama?.Id ?? string.Empty;
        context.FirstEpisode = context.Details?.Episodes.FirstOrDefault(episode =>
            episode.VideoId.Length > 0
        );
        context.VideoId = context.FirstEpisode?.VideoId ?? string.Empty;
        Console.WriteLine(
            $"剧集：{context.SeriesId}，分集：{context.Details?.Episodes.Count ?? 0}，首集：{context.VideoId}，评分：{context.Details?.Drama.Score}"
        );

        context.Medias = await TryWhenIdsAsync(
            "ResolveMediaAsync(ids)",
            context.SeriesId,
            context.VideoId,
            (series, video) => client.ResolveMediaAsync(series, video, cancellationToken)
        );
        PrintMedia(context.Medias);

        long durationMs =
            context.Media?.Duration > TimeSpan.Zero
                ? (long)Math.Ceiling(context.Media.Duration.TotalMilliseconds)
                : 600_000L;
        context.Danmaku = await TryWhenIdsAsync(
            "GetDanmakuAsync",
            context.SeriesId,
            context.VideoId,
            (series, video) =>
                client.GetDanmakuAsync(series, video, 0, durationMs, cancellationToken)
        );
        if (context.Danmaku is not null)
        {
            Console.WriteLine(
                $"  弹幕：{context.Danmaku.Items.Count}/{context.Danmaku.Total}，NextMs={context.Danmaku.NextMs}"
            );
        }

        context.Reviews = await TryWhenIdAsync(
            "GetDramaReviewsAsync",
            context.SeriesId,
            id => client.GetDramaReviewsAsync(id, 10, cancellationToken: cancellationToken)
        );
        PrintComments("  剧评", context.Reviews);

        context.EpisodeComments = await TryWhenIdsAsync(
            "GetEpisodeCommentsAsync(ids)",
            context.SeriesId,
            context.VideoId,
            (series, video) =>
                client.GetEpisodeCommentsAsync(
                    series,
                    video,
                    10,
                    cancellationToken: cancellationToken
                )
        );
        PrintComments("  分集评论", context.EpisodeComments);

        context.SampleComment =
            context.EpisodeComments?.Items.FirstOrDefault(c => c.ReplyCount > 0)
            ?? context.Reviews?.Items.FirstOrDefault(c => c.ReplyCount > 0)
            ?? context.EpisodeComments?.Items.FirstOrDefault()
            ?? context.Reviews?.Items.FirstOrDefault();

        bool isEpisodeComment =
            context.SampleComment is not null
            && context.EpisodeComments?.Items.Contains(context.SampleComment) == true;

        context.Replies = context.SampleComment is null
            ? null
            : await TryAsync(
                isEpisodeComment ? "GetEpisodeCommentRepliesAsync" : "GetDramaReviewRepliesAsync",
                () =>
                    isEpisodeComment
                        ? client.GetEpisodeCommentRepliesAsync(
                            context.SampleComment.Id,
                            context.SeriesId,
                            context.VideoId,
                            cancellationToken: cancellationToken
                        )
                        : client.GetDramaReviewRepliesAsync(
                            context.SampleComment.Id,
                            context.SeriesId,
                            cancellationToken: cancellationToken
                        )
            );
        PrintCount("  评论回复", context.Replies?.Items.Count);

        context.CreditUserId =
            context.Details?.Drama.Credits.FirstOrDefault()?.User.Id ?? string.Empty;
        UserProfile? creditProfile = await TryWhenIdAsync(
            "GetUserProfileAsync(userId)",
            context.CreditUserId,
            id => client.GetUserProfileAsync(id, cancellationToken)
        );
        PrintProfile(creditProfile);

        if (context.Details?.Drama.Credits.FirstOrDefault() is { } credit)
        {
            UserProfile? creditProfileByCredit = await TryAsync(
                "GetUserProfileAsync(credit)",
                () => client.GetUserProfileAsync(credit, cancellationToken)
            );
            PrintProfile(creditProfileByCredit);
        }

        string ordinaryUserId = context.FindOrdinaryUserId();
        context.ProfileUserId = ordinaryUserId.Length > 0 ? ordinaryUserId : context.CreditUserId;
        UserActivityPage? activities = await TryWhenIdAsync(
            "GetUserActivitiesAsync",
            context.ProfileUserId,
            id => client.GetUserActivitiesAsync(id, cancellationToken: cancellationToken)
        );
        PrintCount("  用户动态", activities?.Items.Count);

        if (activities is { HasMore: true, Cursor.Length: > 0 })
        {
            UserActivityPage? nextActivities = await TryAsync(
                "GetUserActivitiesAsync(next)",
                () =>
                    client.GetUserActivitiesAsync(
                        context.ProfileUserId,
                        cursor: activities.Cursor,
                        cancellationToken: cancellationToken
                    )
            );
            PrintCount("  用户动态下一页", nextActivities?.Items.Count);
        }

        UserWorkPage? userWorks = await TryWhenIdAsync(
            "GetUserWorksAsync",
            context.ProfileUserId,
            id => client.GetUserWorksAsync(id, new UserWorksQuery { Count = 10 }, cancellationToken)
        );
        PrintCount("  用户作品", userWorks?.Items.Count);

        UserDramaPage? userCollections = await TryWhenIdAsync(
            "GetUserCollectionsAsync",
            context.ProfileUserId,
            id => client.GetUserCollectionsAsync(id, cancellationToken: cancellationToken)
        );
        PrintCount("  用户收藏", userCollections?.Items.Count);

        UserDramaPage? userLikes = await TryWhenIdAsync(
            "GetUserLikesAsync",
            context.ProfileUserId,
            id => client.GetUserLikesAsync(id, cancellationToken: cancellationToken)
        );
        PrintCount("  用户点赞", userLikes?.Items.Count);

        UserRelationPage? followers = await TryWhenIdAsync(
            "GetUserFollowersAsync",
            context.ProfileUserId,
            id => client.GetUserFollowersAsync(id, count: 10, cancellationToken: cancellationToken)
        );
        PrintCount("  用户粉丝", followers?.Items.Count);

        UserRelationPage? followings = await TryWhenIdAsync(
            "GetUserFollowingsAsync",
            context.ProfileUserId,
            id => client.GetUserFollowingsAsync(id, count: 10, cancellationToken: cancellationToken)
        );
        PrintCount("  用户关注", followings?.Items.Count);
    }

    public static async Task RunUserRelatedAsync(SampleContext context)
    {
        PhoenixReadApiClient client = context.Client;
        CancellationToken cancellationToken = context.CancellationToken;

        context.History = await TryAsync(
            "GetHistoryAsync",
            () => client.GetHistoryAsync(cancellationToken: cancellationToken)
        );
        PrintCount("  历史", context.History?.Items.Count);

        context.Favorites = await TryAsync(
            "GetFavoritesAsync",
            () => client.GetFavoritesAsync(cancellationToken: cancellationToken)
        );
        PrintCount("  收藏", context.Favorites?.Items.Count);

        (IReadOnlyList<string> Ids, DramaPage Page)? favoriteIds = await TryFavoriteIdsAsync(
            "GetFavoriteIdsAsync",
            () => client.GetFavoriteIdsAsync(cancellationToken: cancellationToken)
        );
        PrintCount("  收藏 ID", favoriteIds?.Ids.Count);

        context.Likes = await TryAsync(
            "GetLikesAsync",
            () => client.GetLikesAsync(cancellationToken: cancellationToken)
        );
        PrintCount("  点赞剧集", context.Likes?.Items.Count);

        ReservationPage? reservations = await TryAsync(
            "GetReservationsAsync",
            () => client.GetReservationsAsync(cancellationToken: cancellationToken)
        );
        PrintCount("  预约", reservations?.Items.Count);

        ReservationPage? calendar = await TryAsync(
            "GetCalendarAsync",
            () => client.GetCalendarAsync(cancellationToken: cancellationToken)
        );
        PrintCount("  日历", calendar?.Items.Count);

        ReservationPage? upcomingReservations = await TryAsync(
            "GetUpcomingReservationsAsync",
            () => client.GetUpcomingReservationsAsync(cancellationToken: cancellationToken)
        );
        PrintCount("  可预约新剧", upcomingReservations?.Items.Count);

        FollowUpdatePage? followUpdates = await TryAsync(
            "GetFollowUpdatesAsync",
            () => client.GetFollowUpdatesAsync(cursor: "", count: 5, cancellationToken: cancellationToken)
        );
        PrintCount("  追更短剧", followUpdates?.Items.Count);

        NoticePage? notices = await TryAsync(
            "GetNoticesAsync",
            () => client.GetNoticesAsync(cancellationToken: cancellationToken)
        );
        Console.WriteLine($"  通知中心: {notices?.Items.Count ?? 0} 条，未读 {notices?.UnreadCount ?? 0} 条");

        string sampleActorId = "3974078057440443"; // 黄彦恺
        UserProfile? actorProfile = await TryAsync(
            "GetUserProfileAsync",
            () => client.GetUserProfileAsync(sampleActorId, cancellationToken)
        );
        if (actorProfile is not null)
        {
            Console.WriteLine($"  演职人员主页: {actorProfile.User.Name} ({string.Join(" | ", actorProfile.CreatorRoles)})");
            Console.WriteLine($"    红果号: {actorProfile.HongguoId}, 蓝V: {actorProfile.IsVerified}, 粉丝: {actorProfile.FansCount}, 获赞: {actorProfile.LikedCount}");
            Console.WriteLine($"    首屏作品数: {actorProfile.Works.Count}");
        }
    }

    public static async Task RunShareAsync(SampleContext context)
    {
        PhoenixReadApiClient client = context.Client;
        CancellationToken cancellationToken = context.CancellationToken;

        ShareLink? share = await TryWhenIdsAsync(
            "CreateShareLinkAsync",
            context.SeriesId,
            context.VideoId,
            (series, video) => client.CreateShareLinkAsync(series, video, cancellationToken)
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
                        : $"https://hongguoduanju.com/?series_id={context.SeriesId}&video_id={context.VideoId}"
                )
        );
        if (parsedShare is not null)
        {
            Console.WriteLine($"  分享解析：{parsedShare.SeriesId}/{parsedShare.VideoId}");
        }
    }
}
