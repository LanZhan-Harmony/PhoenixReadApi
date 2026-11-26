using System.Globalization;
using System.Text;
using PhoenixReadApi.Internal.Contracts.App.Catalog;
using PhoenixReadApi.Internal.Contracts.App.Search;
using PhoenixReadApi.Internal.Contracts.Shared.Drama;
using PhoenixReadApi.Internal.Features.Shared;
using PhoenixReadApi.Internal.Infrastructure.Runtime;

namespace PhoenixReadApi.Internal.Features.Catalog;

using Drama = PhoenixReadApi.Drama;

/// <summary>负责 App 首页频道流式瀑布流发现能力。</summary>
internal sealed class DiscoverService : PhoenixFeatureService
{
    internal DiscoverService(PhoenixReadClientRuntime runtime)
        : base(runtime) { }

    internal async Task<DiscoverPage> GetDiscoverPageAsync(
        DiscoverChannel channel,
        string? cursor,
        CancellationToken cancellationToken
    )
    {
        if (!string.IsNullOrWhiteSpace(cursor))
        {
            return await FetchCellChangeAsync(cursor, cancellationToken).ConfigureAwait(false);
        }

        (string requestTab, string bottomTab, int responseTab) = ResolveChannel(channel);

        AppBookmallTabResponse tabResponse = await _appTransport
            .GetRootAsync<AppBookmallTabResponse>(
                "/reading/bookapi/bookmall/tab/v",
                [
                    new("tab_type", requestTab),
                    new("bottom_tab_type", bottomTab),
                    new("bottom_tab_type_list", "7,0,1,2,3,4"),
                    new("offset", "0"),
                    new("client_req_type", "3"),
                    new("user_is_login", UserIsLogin),
                ],
                cancellationToken,
                skipDeviceAndSignature: true
            )
            .ConfigureAwait(false);

        List<Drama> dramas = [];
        List<Novel> novels = [];
        HashSet<string> seenDramas = [with(StringComparer.Ordinal)];
        HashSet<string> seenNovels = [with(StringComparer.Ordinal)];

        IReadOnlyList<AppBookmallTabItemDto> tabItems = tabResponse.Data?.TabItems ?? [];
        AppBookmallTabItemDto? matchedTab = tabItems.FirstOrDefault(t => t.TabType == responseTab)
            ?? tabItems.FirstOrDefault(t =>
                t.TabType.ToString(CultureInfo.InvariantCulture) == requestTab
            )
            ?? tabItems.FirstOrDefault();

        DiscoverCellChangeState? continuation = null;

        if (matchedTab is not null)
        {
            foreach (AppBookmallCellDto cell in matchedTab.CellData ?? [])
            {
                ExtractCellContent(cell, dramas, novels, seenDramas, seenNovels);
            }

            continuation = FindContinuation(matchedTab, responseTab);
        }

        string nextCursor = string.Empty;
        bool hasMore = false;

        if (continuation is not null && !string.IsNullOrWhiteSpace(continuation.CellId))
        {
            if (dramas.Count == 0 && novels.Count == 0)
            {
                // Tab 首屏未直接携带条目时才拉取首批 change 瀑布流，避免常规请求多一次往返
                try
                {
                    AppBookmallCellChangeResponse changeResponse = await FetchCellChangeCoreAsync(
                            continuation,
                            cancellationToken
                        )
                        .ConfigureAwait(false);

                    ExtractCellView(
                        changeResponse.Data.CellView,
                        dramas,
                        novels,
                        seenDramas,
                        seenNovels
                    );

                    hasMore = changeResponse.Data.HasMore;
                    if (hasMore && changeResponse.Data.NextOffset > continuation.Offset)
                    {
                        DiscoverCellChangeState nextState = continuation with
                        {
                            Offset = changeResponse.Data.NextOffset,
                            SessionId = !string.IsNullOrWhiteSpace(changeResponse.Data.SessionId)
                                ? changeResponse.Data.SessionId
                                : continuation.SessionId,
                        };
                        nextCursor = EncodeCursor(nextState);
                    }
                }
                catch (Exception)
                {
                    // 首批 change 失败时回退为已有 tab 内容
                }
            }
            else
            {
                hasMore = true;
                nextCursor = EncodeCursor(continuation);
            }
        }

        return new DiscoverPage
        {
            Dramas = dramas,
            Novels = novels,
            Cursor = nextCursor,
            HasMore = hasMore,
        };
    }

    private async Task<DiscoverPage> FetchCellChangeAsync(
        string cursor,
        CancellationToken cancellationToken
    )
    {
        DiscoverCellChangeState state = DecodeCursor(cursor);
        AppBookmallCellChangeResponse changeResponse = await FetchCellChangeCoreAsync(
                state,
                cancellationToken
            )
            .ConfigureAwait(false);

        List<Drama> dramas = [];
        List<Novel> novels = [];
        HashSet<string> seenDramas = [with(StringComparer.Ordinal)];
        HashSet<string> seenNovels = [with(StringComparer.Ordinal)];

        ExtractCellView(changeResponse.Data.CellView, dramas, novels, seenDramas, seenNovels);

        string nextCursor = string.Empty;
        if (changeResponse.Data.HasMore && changeResponse.Data.NextOffset > state.Offset)
        {
            DiscoverCellChangeState nextState = state with
            {
                Offset = changeResponse.Data.NextOffset,
                SessionId = !string.IsNullOrWhiteSpace(changeResponse.Data.SessionId)
                    ? changeResponse.Data.SessionId
                    : state.SessionId,
            };
            nextCursor = EncodeCursor(nextState);
        }

        return new DiscoverPage
        {
            Dramas = dramas,
            Novels = novels,
            Cursor = nextCursor,
            HasMore = changeResponse.Data.HasMore && nextCursor.Length > 0,
        };
    }

    private Task<AppBookmallCellChangeResponse> FetchCellChangeCoreAsync(
        DiscoverCellChangeState state,
        CancellationToken cancellationToken
    )
    {
        List<KeyValuePair<string, string>> query =
        [
            new("cell_id", state.CellId),
            new("offset", state.Offset.ToString(CultureInfo.InvariantCulture)),
            new("limit", "20"),
            new("change_type", "0"),
            new("tab_type", state.TabType),
            new("show_type", state.ShowType),
            new("client_req_type", "2"),
            new("session_id", state.SessionId),
            new("user_is_login", UserIsLogin),
        ];

        if (!string.IsNullOrWhiteSpace(state.ClientTemplate))
        {
            query.Add(new("client_template", state.ClientTemplate));
        }

        return _appTransport.GetRootAsync<AppBookmallCellChangeResponse>(
            "/reading/bookapi/bookmall/cell/change/v",
            query,
            cancellationToken
        );
    }

    private static void ExtractCellView(
        AppBookmallCellViewDto? cellView,
        List<Drama> dramas,
        List<Novel> novels,
        HashSet<string> seenDramas,
        HashSet<string> seenNovels
    )
    {
        if (cellView is null)
        {
            return;
        }

        foreach (DramaDto dto in cellView.VideoData ?? [])
        {
            AddDramaFromDto(dto, dramas, seenDramas);
        }

        foreach (AppBookmallCellDto cell in cellView.CellData ?? [])
        {
            ExtractCellContent(cell, dramas, novels, seenDramas, seenNovels);
        }

        foreach (AppSearchBookDto book in cellView.BookData ?? [])
        {
            AddNovel(book, novels, seenNovels);
        }
    }

    private static void ExtractCellContent(
        AppBookmallCellDto cell,
        List<Drama> dramas,
        List<Novel> novels,
        HashSet<string> seenDramas,
        HashSet<string> seenNovels
    )
    {
        if (cell is null)
        {
            return;
        }

        foreach (DramaDto dto in cell.VideoData ?? [])
        {
            AddDramaFromDto(dto, dramas, seenDramas);
        }

        if (cell.VideoDetail is { } directDetail)
        {
            if (directDetail.VideoDetailList is { Count: > 0 } nestedDetails)
            {
                foreach (DramaVideoDetailDto item in nestedDetails)
                {
                    AddDramaFromDetail(item, dramas, seenDramas);
                }
            }
            else
            {
                AddDramaFromDetail(directDetail, dramas, seenDramas);
            }
        }

        foreach (AppSearchBookDto book in cell.BookData ?? [])
        {
            AddNovel(book, novels, seenNovels);
        }

        foreach (AppBookmallCellDto nested in cell.CellData ?? [])
        {
            ExtractCellContent(nested, dramas, novels, seenDramas, seenNovels);
        }
    }

    private static void AddDramaFromDto(
        DramaDto? dto,
        List<Drama> dramas,
        HashSet<string> seenDramas
    )
    {
        if (dto is null)
        {
            return;
        }

        Drama drama = DramaMapper.FromDto(dto, "短剧");
        if (drama.Id.Length > 0 && seenDramas.Add(drama.Id))
        {
            dramas.Add(drama);
        }
    }

    private static void AddDramaFromDetail(
        DramaVideoDetailDto? detail,
        List<Drama> dramas,
        HashSet<string> seenDramas
    )
    {
        if (detail is null)
        {
            return;
        }

        Drama drama = DramaMapper.FromVideoDetail(detail, "短剧");
        if (drama.Id.Length > 0 && seenDramas.Add(drama.Id))
        {
            dramas.Add(drama);
        }
    }

    private static void AddNovel(
        AppSearchBookDto? book,
        List<Novel> novels,
        HashSet<string> seenNovels
    )
    {
        if (
            book is null
            || string.IsNullOrWhiteSpace(book.BookId)
            || string.IsNullOrWhiteSpace(book.BookName)
        )
        {
            return;
        }

        if (!seenNovels.Add(book.BookId))
        {
            return;
        }

        List<string> tags = [];
        foreach (DramaTagDto tag in book.Tags ?? [])
        {
            if (!string.IsNullOrWhiteSpace(tag?.Name))
            {
                string trimmed = tag.Name.Trim();
                if (!tags.Contains(trimmed, StringComparer.Ordinal))
                {
                    tags.Add(trimmed);
                }
            }
        }

        foreach (string schemaTag in book.CategorySchema ?? [])
        {
            if (!string.IsNullOrWhiteSpace(schemaTag))
            {
                string trimmed = schemaTag.Trim();
                if (!tags.Contains(trimmed, StringComparer.Ordinal))
                {
                    tags.Add(trimmed);
                }
            }
        }

        (NovelCreationStatus status, string statusText) = book.CreationStatus.Trim() switch
        {
            "1" => (NovelCreationStatus.Ongoing, "连载中"),
            "0" or "2" => (NovelCreationStatus.Completed, "已完结"),
            _ => (NovelCreationStatus.Unknown, string.Empty),
        };

        novels.Add(
            new Novel
            {
                Id = book.BookId,
                Title = book.BookName,
                ShortTitle = book.BookShortName,
                Author = book.Author,
                Description = book.Abstract,
                Status = status,
                StatusText = statusText,
                CoverUrl = book.CoverUrl,
                SerialCount = book.SerialCount,
                ReadCount = book.ReadCount,
                Tags = tags,
            }
        );
    }

    private static DiscoverCellChangeState? FindContinuation(
        AppBookmallTabItemDto tab,
        int expectedTabType
    )
    {
        if (string.IsNullOrWhiteSpace(tab.SessionId))
        {
            return null;
        }

        DiscoverCellChangeState? fallback = null;
        string effectiveTabType =
            tab.TabType != 0
                ? tab.TabType.ToString(CultureInfo.InvariantCulture)
                : expectedTabType.ToString(CultureInfo.InvariantCulture);

        foreach (AppBookmallCellDto cell in tab.CellData ?? [])
        {
            string id = !string.IsNullOrWhiteSpace(cell.CellIdStr) ? cell.CellIdStr : cell.CellId;
            if (string.IsNullOrWhiteSpace(id) || cell.ShowType == 0)
            {
                continue;
            }

            DiscoverCellChangeState state = new()
            {
                CellId = id,
                SessionId = tab.SessionId,
                ShowType = cell.ShowType.ToString(CultureInfo.InvariantCulture),
                TabType = effectiveTabType,
                ClientTemplate = tab.ClientTemplate,
                Offset = cell.NextOffset > 0 ? cell.NextOffset : 0,
            };

            // 拥有嵌套子单元格的 cell 是流式容器
            if ((cell.CellData?.Count ?? 0) > 0)
            {
                return state;
            }

            fallback ??= state;
        }

        return fallback;
    }

    private static (string RequestTab, string BottomTab, int ResponseTab) ResolveChannel(
        DiscoverChannel channel
    )
    {
        return channel switch
        {
            DiscoverChannel.Recommend => ("-1", "7", 16),
            DiscoverChannel.LiveDrama => ("39", "7", 39),
            DiscoverChannel.MotionComic => ("36", "7", 36),
            DiscoverChannel.Explore => ("8", "0", 8),
            DiscoverChannel.Novel => ("2", "0", 2),
            DiscoverChannel.Audiobook => ("5", "0", 5),
            _ => ("-1", "7", 16),
        };
    }

    private static string EncodeCursor(DiscoverCellChangeState state)
    {
        string raw =
            $"{state.CellId}|{state.Offset}|{state.TabType}|{state.ShowType}|{state.ClientTemplate}|{state.SessionId}";
        return Convert
            .ToBase64String(Encoding.UTF8.GetBytes(raw))
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    private static DiscoverCellChangeState DecodeCursor(string cursor)
    {
        try
        {
            string padded = cursor.Replace('-', '+').Replace('_', '/');
            padded = (padded.Length % 4) switch
            {
                2 => padded + "==",
                3 => padded + "=",
                _ => padded,
            };
            string raw = Encoding.UTF8.GetString(Convert.FromBase64String(padded));
            string[] parts = raw.Split('|');
            return new DiscoverCellChangeState
            {
                CellId = parts[0],
                Offset = int.Parse(parts[1], CultureInfo.InvariantCulture),
                TabType = parts.Length > 2 ? parts[2] : string.Empty,
                ShowType = parts.Length > 3 ? parts[3] : string.Empty,
                ClientTemplate = parts.Length > 4 ? parts[4] : string.Empty,
                SessionId = parts.Length > 5 ? parts[5] : string.Empty,
            };
        }
        catch (Exception exception)
        {
            throw new ArgumentException("分页游标无效。", nameof(cursor), exception);
        }
    }

    private sealed record DiscoverCellChangeState
    {
        public string CellId { get; init; } = string.Empty;
        public string SessionId { get; init; } = string.Empty;
        public string ClientTemplate { get; init; } = string.Empty;
        public string ShowType { get; init; } = string.Empty;
        public string TabType { get; init; } = string.Empty;
        public int Offset { get; init; }
    }
}
