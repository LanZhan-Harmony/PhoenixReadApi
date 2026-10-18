using System.Globalization;
using System.Text.Json;
using PhoenixReadApi.Internal.Contracts.App;
using PhoenixReadApi.Internal.Contracts.App.Catalog;
using PhoenixReadApi.Internal.Contracts.App.Comment;
using PhoenixReadApi.Internal.Contracts.App.Library;
using PhoenixReadApi.Internal.Contracts.Shared.Drama;
using PhoenixReadApi.Internal.Features.Shared;
using PhoenixReadApi.Internal.Infrastructure.Runtime;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Features.Library;

/// <summary>阅读历史、进度、收藏、点赞、预约和分类等 App 接口。</summary>
internal sealed class LibraryService : PhoenixFeatureService
{
    private const int MaxPageSize = 50;

    internal LibraryService(PhoenixReadClientRuntime runtime)
        : base(runtime) { }

    internal async Task<ReadHistoryPage> GetHistoryAsync(
        int count,
        int offset,
        bool querySoftDeleted,
        long lastMinReadTimestampMilliseconds,
        CancellationToken cancellationToken
    )
    {
        ValidatePage(count, offset);
        AppResponse<AppHistoryData> response = await _appTransport
            .GetAsync<AppHistoryData>(
                "/reading/bookapi/read_history/list/v",
                [
                    new("book_type", "2"),
                    new("limit", "0"),
                    new("is_first_load", "false"),
                    new("offset", offset.ToString(CultureInfo.InvariantCulture)),
                    new("query_soft_deleted", querySoftDeleted ? "true" : "false"),
                    new(
                        "last_min_read_timestamp_ms",
                        lastMinReadTimestampMilliseconds.ToString(CultureInfo.InvariantCulture)
                    ),
                    new("full_field", "false"),
                ],
                cancellationToken
            )
            .ConfigureAwait(false);
        AppHistoryData data = response.Data ?? new();
        return new ReadHistoryPage
        {
            Items = data
                .Items.Select(MapHistory)
                .Where(static item => item.VideoId.Length > 0)
                .ToArray(),
            Total = data.Total,
            NextOffset = data.NextOffset,
            HasMore = data.HasMore,
        };
    }

    internal async Task<ReadProgressPage> GetReadProgressAsync(CancellationToken cancellationToken)
    {
        AppResponse<JsonElement> response = await _appTransport
            .GetAsync<JsonElement>("/reading/bookapi/read_progress/list/v", [], cancellationToken)
            .ConfigureAwait(false);
        return MapProgress(response.VideoData, response.LocalBookData);
    }

    internal async Task<ReadProgressPage> GetReadProgressForAsync(
        IReadOnlyList<string> seriesOrVideoIds,
        CancellationToken cancellationToken
    )
    {
        if (seriesOrVideoIds.Count == 0 || seriesOrVideoIds.Count > 200)
        {
            throw new ArgumentException(
                "至少提供一个、最多提供 200 个 ID。",
                nameof(seriesOrVideoIds)
            );
        }

        string[] ids = seriesOrVideoIds
            .Select(static id => id.Trim())
            .Where(static id => JsonValueReader.IsNumericId(id))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (ids.Length == 0)
        {
            throw new ArgumentException("播放进度查询 ID 无效。", nameof(seriesOrVideoIds));
        }

        long[] numericIds = ids.Select(long.Parse).ToArray();
        try
        {
            AppResponse<JsonElement> response = await _appTransport
                .PostAsync<AppProgressForPayload, JsonElement>(
                    "/reading/bookapi/read_progress/get/v",
                    new AppProgressForPayload { BookIds = numericIds },
                    commentRequest: false,
                    cancellationToken
                )
                .ConfigureAwait(false);
            return MapProgress(response.VideoData, response.LocalBookData);
        }
        catch (PhoenixReadApiProviderException exception) when (exception.ProviderCode == "100103")
        {
            // Some nodes reject the batch endpoint for anonymous devices. The list endpoint
            // contains the same records and is safe to filter locally.
            ReadProgressPage all = await GetReadProgressAsync(cancellationToken)
                .ConfigureAwait(false);
            HashSet<string> wanted = ids.ToHashSet(StringComparer.Ordinal);
            return new ReadProgressPage
            {
                Items = all
                    .Items.Where(item =>
                        wanted.Contains(item.SeriesId) || wanted.Contains(item.VideoId)
                    )
                    .ToArray(),
                LocalItems = all
                    .LocalItems.Where(item =>
                        wanted.Contains(item.SeriesId) || wanted.Contains(item.VideoId)
                    )
                    .ToArray(),
            };
        }
    }

    internal async Task<PhoenixOperationResult> ReportProgressAsync(
        ProgressUpdate update,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(update);
        string videoId = NormalizeVideoId(update.VideoId);
        string seriesId = NormalizeSeriesId(update.SeriesId);
        var timestamp =
            update.UpdatedAtUnixMilliseconds > 0
                ? update.UpdatedAtUnixMilliseconds
                : DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        AppProgressUpdatePayload payload = new()
        {
            BookType = 2,
            VideoId = videoId,
            SeriesId = seriesId,
            VideoIndex = 1,
            IsDelete = update.UseSoftDelete,
            CurrentPlayPositionMs = Math.Max(0, update.CurrentPositionMs),
            DurationMs = 0,
            CurrentPlayTimeMs = Math.Max(0, update.CurrentPositionMs),
            PlayerAccumulateTotalTimeMs = Math.Max(0, update.CurrentPositionMs),
            PlayerCumulativeTotalDurationMs = Math.Max(0, update.DurationMs),
            ReadTimestampMs = timestamp,
            UpdateTimestampMs = 0,
            UseSoftDelete = update.UseSoftDelete,
            RetainVideoPlayTime = 0,
            OriginNovelBookId = 0,
        };
        AppResponse<JsonElement> response = await _appTransport
            .PostAsync<AppProgressUpdatePayload, JsonElement>(
                "/reading/bookapi/read_history/update/v",
                payload,
                commentRequest: false,
                cancellationToken
            )
            .ConfigureAwait(false);
        return Succeeded(response.Message);
    }

    internal Task<PhoenixOperationResult> DeleteHistoryAsync(
        string seriesId,
        string videoId,
        CancellationToken cancellationToken
    ) =>
        ReportProgressAsync(
            new ProgressUpdate
            {
                SeriesId = seriesId,
                VideoId = videoId,
                UseSoftDelete = true,
            },
            cancellationToken
        );

    internal async Task<DramaPage> GetFavoritesAsync(
        int count,
        int offset,
        CancellationToken cancellationToken
    )
    {
        ValidatePage(count, offset);
        AppResponse<AppShelfListData> response = await _appTransport
            .GetAsync<AppShelfListData>(
                "/reading/bookapi/bookshelf/video/list/v",
                [
                    new("count", count.ToString(CultureInfo.InvariantCulture)),
                    new("limit", count.ToString(CultureInfo.InvariantCulture)),
                    new("offset", offset.ToString(CultureInfo.InvariantCulture)),
                    new("tab_type", "4"),
                ],
                cancellationToken
            )
            .ConfigureAwait(false);
        AppShelfListData data = response.Data ?? new();
        IReadOnlyList<AppShelfItem> shelfItems = data.ShelfItems ?? [];
        IReadOnlyList<AppShelfItem> listItems = data.Items ?? [];
        IReadOnlyList<AppShelfItem> items = shelfItems.Count > 0 ? shelfItems : listItems;
        return new DramaPage
        {
            Items = items.Select(MapShelf).Where(static item => item.Id.Length > 0).ToArray(),
            Total = data.Total > 0 ? data.Total : items.Count,
            NextOffset = data.NextOffset,
            HasMore = data.HasMore,
        };
    }

    internal async Task<DramaPage> GetLikesAsync(
        int count,
        int offset,
        CancellationToken cancellationToken
    )
    {
        ValidatePage(count, offset);
        AppResponse<AppUserActionData> response = await _appTransport
            .GetAsync<AppUserActionData>(
                "/reading/ugc/action/mget/v",
                [
                    new("count", count.ToString(CultureInfo.InvariantCulture)),
                    new("object_type_list", "6,15,10"),
                    new("offset", offset.ToString(CultureInfo.InvariantCulture)),
                    new("action_type", "3"),
                ],
                cancellationToken
            )
            .ConfigureAwait(false);
        AppUserActionData data = response.Data ?? new();
        return new DramaPage
        {
            Items = (data.Items ?? [])
                .Where(static item => item is not null)
                .Select(MapLike)
                .Where(static item => item.Id.Length > 0)
                .ToArray(),
            Total = data.Total,
            NextOffset = data.NextOffset,
            HasMore = data.HasMore,
        };
    }

    internal async Task<FollowUpdatePage> GetFollowUpdatesAsync(
        int count,
        string cursor,
        CancellationToken cancellationToken
    )
    {
        ValidateCount(count);
        cursor = NormalizeCursor(cursor);
        AppResponse<AppFollowData> response = await _appTransport
            .GetAsync<AppFollowData>(
                "/novel/articleapi/relation/get_user_action/v1/",
                [
                    new("action_channel", "1"),
                    new("count", count.ToString(CultureInfo.InvariantCulture)),
                    new("cursor_str", cursor),
                ],
                cancellationToken
            )
            .ConfigureAwait(false);
        AppFollowData data = response.Data ?? new();
        AppFollowListInfo listInfo = data.ListInfo ?? new();
        return new FollowUpdatePage
        {
            Items = data.Items.Select(MapFollow).ToArray(),
            Cursor = listInfo.Cursor,
            HasMore = listInfo.HasMore,
            Total = data.Items.Count,
        };
    }

    internal async Task<ReservationPage> GetReservationsAsync(
        int count,
        int offset,
        bool calendar,
        CancellationToken cancellationToken
    )
    {
        ValidatePage(count, offset);
        AppResponse<AppReservationData> response = await _appTransport
            .GetAsync<AppReservationData>(
                "/reading/user/subscribe/list/v:version/",
                [
                    new("bdhm_bid", "novelread_lynx"),
                    new("offset", offset.ToString(CultureInfo.InvariantCulture)),
                    new("active_panel", "2"),
                    new("session_id", string.Empty),
                    new("version", "1"),
                    new("gender_type", "2"),
                    new("bdhm_pid", "subscribe-list"),
                    new("tab_type", "1"),
                    new("subscribe_list_opt", "0"),
                    new("filter_type", "gender"),
                    new("limit", count.ToString(CultureInfo.InvariantCulture)),
                    new("tab_style", "2"),
                    new("device_platform", "android"),
                    new("is_online", calendar ? "true" : "false"),
                ],
                cancellationToken
            )
            .ConfigureAwait(false);
        return MapReservations(response.Data ?? new());
    }

    internal async Task<ReservationPage> GetUpcomingReservationsAsync(
        int count,
        int offset,
        CancellationToken cancellationToken
    )
    {
        ValidatePage(count, offset);
        AppResponse<AppReservationData> response = await _appTransport
            .GetAsync<AppReservationData>(
                "/reading/user/subscribe/list/v:version/",
                [
                    new("tab_type", "2"),
                    new("req_type", "panel_only"),
                    new("limit", count.ToString(CultureInfo.InvariantCulture)),
                    new("offset", offset.ToString(CultureInfo.InvariantCulture)),
                    new("session_id", string.Empty),
                    new("version", "0"),
                    new("gender_type", "2"),
                ],
                cancellationToken
            )
            .ConfigureAwait(false);
        return MapReservations(response.Data ?? new());
    }

    internal Task<PhoenixOperationResult> SetReservationAsync(
        string seriesId,
        bool active,
        CancellationToken cancellationToken
    ) => SetReservationCoreAsync(seriesId, active, cancellationToken);

    internal async Task<CategoryPage> GetCategoryAsync(
        CategoryQuery query,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(query);
        ValidateCategory(query.Category);
        (string genre, string scene, string displayName) = CategoryInfo(
            query.Category == PhoenixReadApiCategory.All
                ? PhoenixReadApiCategory.RealDrama
                : query.Category
        );
        AppCategoryPayload payload = new()
        {
            RequestScene = scene,
            Offset = Math.Max(0, query.Offset),
            SessionId = query.SessionId ?? string.Empty,
            ClientRequestType = query.Offset > 0 ? 2 : 3,
            SelectedItems = new AppCategorySelectedItems
            {
                Genre = [genre],
                CategoryTheme = query.ThemeIds,
                CategoryRole = query.RoleIds,
                CategoryEpoch = query.EpochIds,
                OnlineTime = query.OnlineTimeIds,
                CreationStatus = query.StatusIds,
            },
        };
        AppResponse<AppCatalogData> response = await _appTransport
            .PostAsync<AppCategoryPayload, AppCatalogData>(
                "/reading/distribution/category/landpage/v1/",
                payload,
                commentRequest: false,
                cancellationToken
            )
            .ConfigureAwait(false);
        AppCatalogData data = response.Data ?? new();
        return new CategoryPage
        {
            Items = data.Items.Select(item => DramaMapper.FromDto(item, displayName)).ToArray(),
            NextOffset = data.NextOffset,
            SessionId = data.SessionId,
            HasMore = data.HasMore,
        };
    }

    internal async Task<FeedPage> GetNewSeriesAsync(
        int offset,
        string sessionId,
        CancellationToken cancellationToken
    )
    {
        if (offset < 0 || offset > 1_000_000)
        {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }
        try
        {
            AppResponse<AppCellChangeData> response = await _appTransport
                .GetAsync<AppCellChangeData>(
                    "/reading/bookapi/bookmall/cell/change/v1/",
                    [
                        new("selected_items", ""),
                        new("client_req_type", offset > 0 ? "2" : "3"),
                        new("cell_id", ""),
                        new("offset", offset.ToString(CultureInfo.InvariantCulture)),
                        new("session_id", sessionId ?? string.Empty),
                    ],
                    cancellationToken
                )
                .ConfigureAwait(false);
            AppCellChangeData data = response.Data ?? new();
            return MapFeed(data.Cells, data.NextOffset, data.SessionId, data.HasMore);
        }
        catch (PhoenixReadApiProviderException exception) when (exception.ProviderCode == "100103")
        {
            return await GetFeedCategoriesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    internal async Task<FeedPage> GetFeedCategoriesAsync(CancellationToken cancellationToken)
    {
        AppResponse<AppFeedTabData> response = await _appTransport
            .GetAsync<AppFeedTabData>(
                "/reading/bookapi/bookmall/tab/v",
                [
                    new("bottom_tab_type", "0"),
                    new("landing_bottom_tab_type", "0"),
                    new("need_personal_recommend", "true"),
                    new("screen_width_px", "1080"),
                    new("is_horizontal_screen", "false"),
                    new("disable_digg_stat", "false"),
                    new("has_video_cache", "false"),
                    new("last_tab_index", "0"),
                    new("req_rank_category_id", ""),
                    new("video_type_preferences_str", ""),
                    new("tab_type", "0"),
                    new("client_req_type", "3"),
                ],
                cancellationToken
            )
            .ConfigureAwait(false);
        AppFeedTabData data = response.Data ?? new();
        List<FeedSection> sections = [];
        foreach (AppFeedTab tab in data.Tabs ?? [])
        {
            sections.AddRange(MapFeedSections(tab.Cells ?? [], tab.Title));
        }
        return new FeedPage { Sections = sections };
    }

    internal async Task<BookPackFields> GetBookPackFieldsAsync(
        string channel,
        CancellationToken cancellationToken
    )
    {
        channel = (channel ?? string.Empty).Trim();
        if (channel.Length == 0 || channel.Length > 64)
        {
            throw new ArgumentException("书架字段频道无效。", nameof(channel));
        }
        AppResponse<JsonElement> response;
        try
        {
            response = await _appTransport
                .PostAsync<AppBookPackFieldsPayload, JsonElement>(
                    "/reading/distribution/book_pack_fields/v1/",
                    new AppBookPackFieldsPayload { Channel = channel },
                    commentRequest: false,
                    cancellationToken
                )
                .ConfigureAwait(false);
        }
        catch (PhoenixReadApiProviderException exception) when (exception.ProviderCode == "100001")
        {
            return new BookPackFields();
        }
        Dictionary<string, string> values = new(StringComparer.Ordinal);
        if (response.Data.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty property in response.Data.EnumerateObject())
            {
                values[property.Name] =
                    property.Value.ValueKind == JsonValueKind.String
                        ? property.Value.GetString() ?? string.Empty
                        : property.Value.GetRawText();
            }
        }
        return new BookPackFields { Values = values };
    }

    internal async Task<IReadOnlyList<Drama>> GetRelatedDramasAsync(
        string seriesId,
        int totalEpisodeCount,
        int currentEpisode,
        CancellationToken cancellationToken
    )
    {
        seriesId = NormalizeSeriesId(seriesId);
        AppResponse<JsonElement> response;
        try
        {
            response = await _appTransport
                .GetAsync<JsonElement>(
                    "/reading/bookapi/plan/v",
                    [
                        new("book_id", seriesId),
                        new(
                            "total_chapter_num",
                            Math.Max(0, totalEpisodeCount).ToString(CultureInfo.InvariantCulture)
                        ),
                        new(
                            "current_chapter_num",
                            Math.Max(0, currentEpisode).ToString(CultureInfo.InvariantCulture)
                        ),
                        new("is_horizontal_screen", "false"),
                        new("bookstore_tab_type", "4"),
                    ],
                    cancellationToken
                )
                .ConfigureAwait(false);
        }
        catch (PhoenixReadApiProviderException exception) when (exception.ProviderCode == "100103")
        {
            return [];
        }
        List<Drama> result = [];
        if (response.Data.ValueKind != JsonValueKind.Undefined)
        {
            CollectRelatedDramas(response.Data, result);
        }
        return result;
    }

    internal async Task<ShareLink> CreateShareLinkAsync(
        string seriesId,
        string videoId,
        CancellationToken cancellationToken
    )
    {
        seriesId = NormalizeSeriesId(seriesId);
        videoId = NormalizeVideoId(videoId);
        AppResponse<AppShareData> response;
        try
        {
            response = await _appTransport
                .GetAsync<AppShareData>(
                    "/reading/user/share/short_url/unlimited_selector_change_type/",
                    [
                        new("book_id", seriesId),
                        new("video_id", videoId),
                        new("content_id", videoId),
                        new("src_material_id", seriesId),
                        new("source_channel", "short_video"),
                        new("share_type", "video"),
                        new("client_req_type", "3"),
                        new("unlimited_selector_change_type", "0"),
                        new(
                            "share_timestamp",
                            DateTimeOffset
                                .UtcNow.ToUnixTimeSeconds()
                                .ToString(CultureInfo.InvariantCulture)
                        ),
                    ],
                    cancellationToken
                )
                .ConfigureAwait(false);
        }
        catch (PhoenixReadApiHttpException exception) when (exception.StatusCode == 404)
        {
            return new ShareLink { SeriesId = seriesId, VideoId = videoId };
        }
        catch (PhoenixReadApiProviderException exception) when (exception.ProviderCode == "100103")
        {
            return new ShareLink { SeriesId = seriesId, VideoId = videoId };
        }
        AppShareData data = response.Data ?? new();
        return new ShareLink
        {
            SeriesId = seriesId,
            VideoId = videoId,
            Url = data.Url,
            ShortUrl = data.ShortUrl,
            Title = data.Title,
        };
    }

    internal static ShareLink ResolveShareLink(string value)
    {
        if (!Uri.TryCreate(value?.Trim(), UriKind.Absolute, out Uri? uri))
        {
            throw new ArgumentException("分享链接不是有效的绝对 URL。", nameof(value));
        }

        Dictionary<string, string> query = ParseQuery(uri.Query);
        string seriesId = FirstQuery(query, "series_id", "book_id", "group_id");
        string videoId = FirstQuery(query, "video_id", "vid", "item_id");
        return new ShareLink
        {
            Url = uri.ToString(),
            ShortUrl = uri.ToString(),
            SeriesId = JsonValueReader.IsNumericId(seriesId) ? seriesId : string.Empty,
            VideoId = JsonValueReader.IsNumericId(videoId) ? videoId : string.Empty,
        };
    }

    internal async Task<PhoenixOperationResult> SetFavoriteAsync(
        string seriesId,
        string videoId,
        bool active,
        CancellationToken cancellationToken
    )
    {
        seriesId = NormalizeSeriesId(seriesId);
        videoId = NormalizeVideoId(videoId);
        AppShelfUpdatePayload payload = new()
        {
            IsActiveBehavior = true,
            OperationType = active ? 1 : 2,
            Items = [new AppShelfUpdateItem { SeriesId = seriesId, VideoId = videoId }],
        };
        AppResponse<JsonElement> response = await _appTransport
            .PostAsync<AppShelfUpdatePayload, JsonElement>(
                "/reading/bookapi/bookshelf/video/update/v",
                payload,
                commentRequest: false,
                cancellationToken
            )
            .ConfigureAwait(false);
        return Succeeded(response.Message);
    }

    internal async Task<PhoenixOperationResult> SetFollowAsync(
        string userId,
        bool active,
        CancellationToken cancellationToken
    )
    {
        if (!JsonValueReader.IsNumericId(userId = (userId ?? string.Empty).Trim()))
        {
            throw new ArgumentException("账号 ID 无效。", nameof(userId));
        }
        AppResponse<JsonElement> response = await _appTransport
            .PostAsync<AppCommentActionPayload, JsonElement>(
                "/novel/commentapi/comment/do_action/v1/",
                new AppCommentActionPayload
                {
                    ObjectId = userId,
                    ObjectType = 1,
                    ActionType = 25,
                    CommentType = 0,
                    BusinessParameter = new AppCommentActionBusinessParameter
                    {
                        SharkParameter = new AppSharkParameter
                        {
                            EnterFrom = "ShortSeriesActivity",
                            PreviousPage = "MainFragmentActivity",
                            PageList = "MainFragmentActivity,ShortSeriesActivity",
                        },
                    },
                },
                commentRequest: false,
                cancellationToken
            )
            .ConfigureAwait(false);
        return Succeeded(response.Message);
    }

    internal async Task<PhoenixOperationResult> SetDramaLikeAsync(
        string seriesId,
        string videoId,
        bool active,
        CancellationToken cancellationToken
    )
    {
        seriesId = NormalizeSeriesId(seriesId);
        videoId = NormalizeVideoId(videoId);
        AppResponse<JsonElement> response = await _appTransport
            .PostAsync<AppArticleActionPayload, JsonElement>(
                "/novel/articleapi/do_action/v1/",
                new AppArticleActionPayload
                {
                    ObjectId = videoId,
                    ObjectType = 6,
                    ActionType = active ? 3 : 4,
                    ActionCategory = 1,
                    VideoId = seriesId,
                    BusinessParameter = new AppArticleActionBusinessParameter
                    {
                        BookId = 0,
                        VideoId = seriesId,
                        HasAigcContent = false,
                        ModifyCount = 0,
                        ScreenFrame = string.Empty,
                    },
                },
                commentRequest: false,
                cancellationToken
            )
            .ConfigureAwait(false);
        return Succeeded(response.Message);
    }

    private async Task<PhoenixOperationResult> SetReservationCoreAsync(
        string seriesId,
        bool active,
        CancellationToken cancellationToken
    )
    {
        seriesId = NormalizeSeriesId(seriesId);
        AppResponse<JsonElement> response = await _appTransport
            .PostAsync<AppReservationActionPayload, JsonElement>(
                active
                    ? "/reading/bookapi/search/uncover_subscribe/v"
                    : "/reading/user/subscribe/delete/v",
                new AppReservationActionPayload
                {
                    ItemId = seriesId,
                    ItemType = 14,
                    OperationType = active ? 1 : 2,
                    WishListAllDelete = 0,
                    NotDeleteItemId = active ? string.Empty : seriesId,
                    SharkParameter = new AppSharkParameter
                    {
                        EnterFrom = "ShortSeriesActivity",
                        PreviousPage = "MainFragmentActivity",
                        PageList = "MainFragmentActivity,ShortSeriesActivity",
                    },
                },
                commentRequest: false,
                cancellationToken
            )
            .ConfigureAwait(false);
        return Succeeded(response.Message);
    }

    private static ReadHistoryEntry MapHistory(AppHistoryItem item)
    {
        AppHistoryVideoData video = item.Video ?? new();
        return new ReadHistoryEntry
        {
            Id = item.Id,
            SeriesId = JsonValueReader.First(
                item.BookIdString,
                item.SeriesId,
                item.BookId,
                video.SeriesId
            ),
            VideoId = JsonValueReader.First(item.VideoId, item.VideoIdAlias, video.VideoId),
            EpisodeNumber = item.EpisodeNumber > 0 ? item.EpisodeNumber : video.EpisodeNumber,
            Title = JsonValueReader.First(item.BookName, item.Title, video.Title),
            CoverUrl = JsonValueReader.First(item.ThumbUrl, item.CoverUrl, video.CoverUrl),
            CurrentPositionMs = Math.Max(item.CurrentPositionMs, item.CurrentPositionAlias),
            DurationMs = Math.Max(0, item.DurationMs),
            UpdatedAtUnixMilliseconds = Math.Max(
                Math.Max(0, item.UpdatedAtUnixMilliseconds),
                Math.Max(0, item.ReadTimestampMilliseconds)
            ),
        };
    }

    private static ReadProgressPage MapProgress(
        IReadOnlyList<AppReadProgressItem>? items,
        IReadOnlyList<AppReadProgressItem>? localItems
    )
    {
        items ??= [];
        localItems ??= [];
        return new ReadProgressPage
        {
            Items = items.Select(MapProgress).ToArray(),
            LocalItems = localItems.Select(MapProgress).ToArray(),
        };
    }

    private static ReadProgress MapProgress(AppReadProgressItem item) =>
        new()
        {
            SeriesId = item.SeriesId,
            VideoId = item.VideoId,
            EpisodeNumber = item.EpisodeNumber,
            EpisodeCount = item.EpisodeCount,
            LastWatchTimeUnixMilliseconds = item.LastWatchTime,
            CurrentPositionMs = item.CurrentPlayTime,
            DurationMs = item.CurrentTotalTime,
            CumulativeDurationMs = item.CumulativeDuration,
        };

    private static Drama MapShelf(AppShelfItem item)
    {
        AppShelfVideoData video = item.Video ?? new();
        string seriesId = JsonValueReader.First(item.SeriesId, video.SeriesId);
        // The real bookshelf list currently returns only series_id for short dramas.
        // Keep missing metadata empty instead of presenting the ID as a title; callers
        // can request full details with GetDramaDetailsAsync(seriesId).
        return new Drama
        {
            Id = seriesId,
            Title = item.Title,
            Description = item.Description,
            CoverUrl = item.CoverUrl,
            EpisodeCount = video.EpisodeCount,
        };
    }

    private static Drama MapLike(AppUserActionRecord item)
    {
        AppUserActionVideoData video = item.Video ?? new();
        AppUserActionSeriesDetail series = video.Series ?? new();
        string id = JsonValueReader.First(series.SeriesId, video.SeriesId);
        return DramaMapper.FromAction(
            id,
            series.Title,
            JsonValueReader.First(series.Introduction, video.Description),
            JsonValueReader.First(series.CoverUrl, video.CoverUrl),
            Math.Max(series.EpisodeCount, video.EpisodeCount),
            series.Status,
            series.Tags
        );
    }

    private static FollowUpdate MapFollow(AppFollowItem item)
    {
        AppFollowVideo video = item.Video ?? new();
        return new FollowUpdate
        {
            Id = JsonValueReader.First(item.Id, video.SeriesId),
            UserId = item.UserId,
            Text = JsonValueReader.First(item.Text, video.Title),
            CoverUrl = JsonValueReader.First(item.CoverUrl, video.CoverUrl),
            CreatedAtUnixSeconds = Math.Max(0, item.CreatedAt),
            Drama = DramaMapper.FromRelatedDrama(
                video.SeriesId,
                video.Title,
                string.Empty,
                video.CoverUrl,
                0
            ),
        };
    }

    private static ReservationPage MapReservations(AppReservationData data)
    {
        IReadOnlyList<AppReservationItem> items =
            data.Items is { Count: > 0 } ? data.Items
            : data.SubscribeItems is { Count: > 0 } ? data.SubscribeItems
            : data.LegacySubscribeItems ?? [];
        return new ReservationPage
        {
            Items = items.Select(MapReservation).ToArray(),
            NextOffset = data.NextOffset,
            HasMore = data.HasMore,
            Total = Math.Max(
                items.Count,
                Math.Max(data.Total, Math.Max(data.OnlineTotalCount, data.OfflineTotalCount))
            ),
        };
    }

    private static Reservation MapReservation(AppReservationItem item) =>
        new()
        {
            Id = JsonValueReader.First(item.Id, item.ItemId),
            SeriesId = JsonValueReader.First(item.SeriesId, item.ItemId),
            VideoId = item.VideoId,
            Title = JsonValueReader.First(item.Title, item.Name),
            CoverUrl = item.CoverUrl,
            ReservedAtUnixSeconds = Math.Max(0, item.ReservedAt),
            ReleaseAtUnixSeconds = Math.Max(item.ReleaseAt, item.ExpectedPublishAt),
            IsActive = item.IsOnline || item.ReleaseAt > 0,
        };

    private static FeedPage MapFeed(
        IReadOnlyList<AppFeedCell>? cells,
        int nextOffset,
        string sessionId,
        bool hasMore
    ) =>
        new()
        {
            Sections = MapFeedSections(cells ?? [], ""),
            NextOffset = nextOffset,
            SessionId = sessionId ?? string.Empty,
            HasMore = hasMore,
        };

    private static void CollectRelatedDramas(JsonElement value, List<Drama> result)
    {
        if (value.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement item in value.EnumerateArray())
            {
                CollectRelatedDramas(item, result);
            }
            return;
        }
        if (value.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        string id = ReadJsonString(value, "series_id", "book_id", "id");
        string title = ReadJsonString(value, "series_title", "title", "book_name");
        if (
            JsonValueReader.IsNumericId(id)
            && title.Length > 0
            && result.All(item => item.Id != id)
        )
        {
            result.Add(
                new Drama
                {
                    Id = id,
                    Title = title,
                    Description = ReadJsonString(value, "series_intro", "intro", "description"),
                    CoverUrl = ReadJsonString(value, "series_cover", "cover", "cover_url"),
                    Category = ReadJsonString(value, "category"),
                    EpisodeCount = ReadJsonInt(value, "episode_cnt", "episode_count"),
                }
            );
        }

        foreach (JsonProperty property in value.EnumerateObject())
        {
            CollectRelatedDramas(property.Value, result);
        }
    }

    private static string ReadJsonString(JsonElement value, params string[] names)
    {
        foreach (string name in names)
        {
            if (value.TryGetProperty(name, out JsonElement item))
            {
                return item.ValueKind == JsonValueKind.String
                    ? item.GetString() ?? string.Empty
                    : item.ToString();
            }
        }
        return string.Empty;
    }

    private static int ReadJsonInt(JsonElement value, params string[] names)
    {
        string text = ReadJsonString(value, names);
        return int.TryParse(
            text,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out int result
        )
            ? result
            : 0;
    }

    private static IReadOnlyList<FeedSection> MapFeedSections(
        IReadOnlyList<AppFeedCell> cells,
        string parentTitle
    )
    {
        List<FeedSection> sections = [];
        foreach (AppFeedCell cell in cells ?? [])
        {
            IReadOnlyList<DramaDto> videoItems = cell.VideoItems ?? [];
            IReadOnlyList<DramaDto> bookItems = cell.BookItems ?? [];
            IReadOnlyList<DramaDto> rows = videoItems.Count > 0 ? videoItems : bookItems;
            List<Drama> items = rows.Select(item => DramaMapper.FromDto(item, "推荐"))
                .Where(static item => item.Id.Length > 0)
                .ToList();
            string title = cell.Name.Length > 0 ? cell.Name : parentTitle;
            if (items.Count > 0)
            {
                sections.Add(
                    new FeedSection
                    {
                        Id = cell.Id,
                        Title = title,
                        Items = items,
                    }
                );
            }
            sections.AddRange(MapFeedSections(cell.Children ?? [], title));
        }
        return sections;
    }

    private static PhoenixOperationResult Succeeded(string message) =>
        new() { Succeeded = true, Message = message ?? string.Empty };

    private static void ValidatePage(int count, int offset)
    {
        ValidateCount(count);
        if (offset is < 0 or > 1_000_000)
        {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }
    }

    private static void ValidateCount(int count)
    {
        if (count is < 1 or > MaxPageSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(count),
                $"数量必须在 1 到 {MaxPageSize} 之间。"
            );
        }
    }

    private static string NormalizeCursor(string value)
    {
        value = (value ?? string.Empty).Trim();
        if (value.Length > 8192 || value.Any(static c => c is '\r' or '\n' or '\0'))
        {
            throw new ArgumentException("游标无效。", nameof(value));
        }
        return value;
    }

    private static Dictionary<string, string> ParseQuery(string query)
    {
        Dictionary<string, string> values = new(StringComparer.OrdinalIgnoreCase);
        foreach (
            string item in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries)
        )
        {
            string[] parts = item.Split('=', 2);
            if (parts.Length == 2)
            {
                values[Uri.UnescapeDataString(parts[0])] = Uri.UnescapeDataString(parts[1]);
            }
        }
        return values;
    }

    private static string FirstQuery(
        IReadOnlyDictionary<string, string> values,
        params string[] names
    )
    {
        foreach (string name in names)
        {
            if (values.TryGetValue(name, out string? value) && value.Length > 0)
            {
                return value;
            }
        }
        return string.Empty;
    }
}
