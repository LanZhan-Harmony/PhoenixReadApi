using PhoenixReadApi.Internal.Contracts.App;
using PhoenixReadApi.Internal.Contracts.App.Catalog;
using PhoenixReadApi.Internal.Contracts.Shared.Drama;
using PhoenixReadApi.Internal.Features.Shared;
using PhoenixReadApi.Internal.Infrastructure.Runtime;

namespace PhoenixReadApi.Internal.Features.Catalog;

using Drama = global::PhoenixReadApi.Drama;

/// <summary>负责短剧动态多维筛选面板与矩阵内容检索服务。</summary>
internal sealed class ShortDramaFilterService : PhoenixFeatureService
{
    internal ShortDramaFilterService(PhoenixReadClientRuntime runtime)
        : base(runtime) { }

    internal async Task<IReadOnlyList<ShortDramaFilterGroup>> GetFilterPanelAsync(
        ShortDramaGenre genre,
        CancellationToken cancellationToken
    )
    {
        string scene = ResolveScene(genre);
        AppShortFilterPayload payload = new()
        {
            ReqScene = scene,
            ReqType = "only_panel",
            NeedSelectorPanel = false,
        };

        AppResponse<AppShortFilterData> response = await _appTransport
            .PostAsync<AppShortFilterPayload, AppShortFilterData>(
                "/reading/distribution/category/landpage/v1/",
                payload,
                commentRequest: false,
                cancellationToken
            )
            .ConfigureAwait(false);

        AppShortFilterData? data = response.Data;
        if (data is null || data.SelectorRows.Count == 0)
        {
            throw new PhoenixReadApiResponseException(
                "筛选面板数据不存在或暂不可用。",
                "filter_panel"
            );
        }

        List<ShortDramaFilterGroup> rows = [];
        foreach (AppShortFilterRowDto rowDto in data.SelectorRows)
        {
            if (string.IsNullOrWhiteSpace(rowDto.Type))
            {
                continue;
            }

            List<ShortDramaFilterItem> items = [];
            foreach (AppShortFilterItemDto item in rowDto.Items)
            {
                if (!string.IsNullOrWhiteSpace(item.SelectorItemId))
                {
                    items.Add(
                        new ShortDramaFilterItem
                        {
                            Name = item.ShowName,
                            Value = item.SelectorItemId,
                        }
                    );
                }
            }

            rows.Add(
                new ShortDramaFilterGroup
                {
                    Key = rowDto.Type,
                    Name = rowDto.RowName,
                    Items = items,
                }
            );
        }

        return rows;
    }

    internal async Task<ShortDramaFilterPage> FilterShortDramasAsync(
        ShortDramaFilterQuery query,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(query);
        string scene = ResolveScene(query.Genre);

        AppShortFilterPayload payload = new()
        {
            ReqScene = scene,
            ReqType = "only_content",
            NeedSelectorPanel = false,
            Offset = Math.Max(0, query.Offset),
            Limit = 18,
            SessionId = query.SessionId ?? string.Empty,
            SelectItems = query.Selections,
        };

        AppResponse<AppShortFilterData> response = await _appTransport
            .PostAsync<AppShortFilterPayload, AppShortFilterData>(
                "/reading/distribution/category/landpage/v1/",
                payload,
                commentRequest: false,
                cancellationToken
            )
            .ConfigureAwait(false);

        AppShortFilterData? data = response.Data;
        List<Drama> items = [];
        HashSet<string> seen = new(StringComparer.Ordinal);
        foreach (DramaDto dto in data?.VideoData ?? [])
        {
            Drama drama = DramaMapper.FromDto(dto, "短剧");
            if (drama.Id.Length > 0 && seen.Add(drama.Id))
            {
                items.Add(drama);
            }
        }

        int nextOffset = data?.NextOffset ?? 0;
        if (nextOffset == 0 && items.Count > 0)
        {
            nextOffset = query.Offset + items.Count;
        }

        return new ShortDramaFilterPage
        {
            Items = items,
            NextOffset = nextOffset,
            HasMore = data?.HasMore ?? false,
            SessionId = data?.SessionId ?? string.Empty,
        };
    }

    private static string ResolveScene(ShortDramaGenre genre) =>
        genre switch
        {
            ShortDramaGenre.ShortPlay => "default",
            ShortDramaGenre.ComicSeries => "comic_series",
            ShortDramaGenre.AiSeries => "ai_series",
            _ => "default",
        };
}
