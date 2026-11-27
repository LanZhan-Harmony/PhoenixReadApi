using System.Globalization;
using PhoenixReadApi.Internal.Contracts.App;
using PhoenixReadApi.Internal.Contracts.App.Comment;
using PhoenixReadApi.Internal.Contracts.App.Library;
using PhoenixReadApi.Internal.Features.Search;
using PhoenixReadApi.Internal.Infrastructure.Http;
using PhoenixReadApi.Internal.Infrastructure.Runtime;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Features.Library;

using Drama = PhoenixReadApi.Drama;
using Post = PhoenixReadApi.Post;

/// <summary>剧集收藏列表以及收藏和取消收藏操作。</summary>
internal sealed class FavoriteService : LibraryServiceBase
{
    internal FavoriteService(PhoenixReadClientRuntime runtime)
        : base(runtime) { }

    internal async Task<PhoenixOperationResult> SetFavoriteAsync(
        string dramaId,
        string videoId,
        bool active,
        CancellationToken cancellationToken
    )
    {
        dramaId = NormalizeDramaId(dramaId);
        videoId = NormalizeVideoId(videoId);
        AppShelfUpdatePayload payload = new()
        {
            IsActiveBehavior = true,
            OperationType = active ? 1 : 2,
            Items = [new AppShelfUpdateItem { SeriesId = dramaId, VideoId = videoId }],
        };
        AppResponse<AppOperationResponse> response = await _appTransport
            .PostAsync<AppShelfUpdatePayload, AppOperationResponse>(
                "/reading/bookapi/bookshelf/video/update/v",
                payload,
                commentRequest: false,
                cancellationToken,
                retryMode: AppRequestRetryMode.NoRetry
            )
            .ConfigureAwait(false);
        return Succeeded(response.Message);
    }

    internal Task<PhoenixOperationResult> SetShortVideoFavoriteAsync(
        string videoId,
        bool active,
        CancellationToken cancellationToken
    ) => SetBookFavoriteAsync(videoId, bookType: 4, active, cancellationToken);

    internal Task<PhoenixOperationResult> SetDramaAlbumFavoriteAsync(
        string albumId,
        bool active,
        CancellationToken cancellationToken
    ) => SetBookFavoriteAsync(albumId, bookType: 3, active, cancellationToken);

    internal async Task<PhoenixOperationResult> SetPostFavoriteAsync(
        string postId,
        bool active,
        CancellationToken cancellationToken
    )
    {
        postId = NormalizeVideoId(postId);
        AppResponse<AppOperationResponse> response = await _appTransport
            .PostAsync<AppArticleActionPayload, AppOperationResponse>(
                "/novel/articleapi/do_action/v1/",
                new AppArticleActionPayload
                {
                    ObjectId = postId,
                    ObjectType = 16,
                    ActionType = active ? 25 : 26,
                    ActionCategory = 1,
                    ActionReasonRemark = string.Empty,
                    BusinessParameter = new AppArticleActionBusinessParameter
                    {
                        BookId = 0,
                        HasAigcContent = false,
                        ModifyCount = 0,
                        SharkParameter = new AppSharkParameter(),
                    },
                },
                commentRequest: false,
                cancellationToken,
                retryMode: AppRequestRetryMode.NoRetry
            )
            .ConfigureAwait(false);
        return Succeeded(response.Message);
    }

    private async Task<PhoenixOperationResult> SetBookFavoriteAsync(
        string bookId,
        int bookType,
        bool active,
        CancellationToken cancellationToken
    )
    {
        bookId = NormalizeVideoId(bookId);
        AppShelfBookUpdatePayload payload = new()
        {
            IsCancelled = false,
            Items =
            [
                new AppShelfBookUpdateItem
                {
                    BookId = bookId,
                    BookType = bookType,
                    ModifyTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    OperationType = active ? 0 : 1,
                },
            ],
        };
        AppResponse<AppOperationResponse> response = await _appTransport
            .PostAsync<AppShelfBookUpdatePayload, AppOperationResponse>(
                "/reading/bookapi/bookshelf/video/update/v",
                payload,
                commentRequest: false,
                cancellationToken,
                retryMode: AppRequestRetryMode.NoRetry
            )
            .ConfigureAwait(false);
        return Succeeded(response.Message);
    }

    internal async Task<IReadOnlyList<Novel>> GetNovelFavoritesAsync(
        CancellationToken cancellationToken
    )
    {
        AppBookShelfListData data = await FetchBookShelfAsync(cancellationToken)
            .ConfigureAwait(false);
        return
        [
            .. (data.Items ?? [])
                .Where(static raw => !string.IsNullOrWhiteSpace(raw.BookId) && raw.BookType != 1)
                .Select(raw => new Novel
                {
                    Id = raw.BookId,
                    SerialCount = raw.SerialCount,
                    Tags = ParseTags(raw.Tags),
                }),
        ];
    }

    internal async Task<IReadOnlyList<Audiobook>> GetAudiobookFavoritesAsync(
        CancellationToken cancellationToken
    )
    {
        AppBookShelfListData data = await FetchBookShelfAsync(cancellationToken)
            .ConfigureAwait(false);
        return
        [
            .. (data.Items ?? [])
                .Where(static raw => !string.IsNullOrWhiteSpace(raw.BookId) && raw.BookType == 1)
                .Select(raw => new Audiobook
                {
                    Id = raw.BookId,
                    ListenBookId = raw.BookId,
                    SerialCount = raw.SerialCount,
                    Tags = ParseTags(raw.Tags),
                }),
        ];
    }

    internal async Task<IReadOnlyList<Drama>> GetDramaFavoritesAsync(
        CancellationToken cancellationToken
    )
    {
        IReadOnlyList<AppShelfItem> rawItems = await FetchVideoShelfAsync(cancellationToken)
            .ConfigureAwait(false);
        List<Drama> dramas = [];
        foreach (AppShelfItem raw in rawItems)
        {
            if (raw.BookType is 3 or 4)
            {
                continue;
            }

            AppShelfVideoData video = raw.Video ?? new();
            string id = ValueNormalizer.First(raw.SeriesId, video.SeriesId);
            if (id.Length == 0)
            {
                continue;
            }

            dramas.Add(
                new Drama
                {
                    Id = id,
                    Title = raw.Title,
                    Description = raw.Description,
                    CoverUrl = raw.CoverUrl,
                    EpisodeCount = video.EpisodeCount,
                }
            );
        }

        return dramas;
    }

    internal async Task<IReadOnlyList<DramaAlbum>> GetAlbumFavoritesAsync(
        CancellationToken cancellationToken
    )
    {
        IReadOnlyList<AppShelfItem> rawItems = await FetchVideoShelfAsync(cancellationToken)
            .ConfigureAwait(false);
        List<DramaAlbum> albums = [];
        foreach (AppShelfItem raw in rawItems)
        {
            if (raw.BookType != 3)
            {
                continue;
            }

            AppShelfVideoData video = raw.Video ?? new();
            string id = ValueNormalizer.First(raw.SeriesId, video.SeriesId);
            if (id.Length == 0)
            {
                continue;
            }

            albums.Add(
                new DramaAlbum
                {
                    Id = id,
                    Title = raw.Title,
                    Description = raw.Description,
                    CoverUrl = raw.CoverUrl,
                }
            );
        }

        return albums;
    }

    internal async Task<IReadOnlyList<ShortVideo>> GetShortVideoFavoritesAsync(
        CancellationToken cancellationToken
    )
    {
        IReadOnlyList<AppShelfItem> rawItems = await FetchVideoShelfAsync(cancellationToken)
            .ConfigureAwait(false);
        List<ShortVideo> shortVideos = [];
        foreach (AppShelfItem raw in rawItems)
        {
            if (raw.BookType != 4)
            {
                continue;
            }

            AppShelfVideoData video = raw.Video ?? new();
            string id = ValueNormalizer.First(raw.SeriesId, video.SeriesId);
            if (id.Length == 0)
            {
                continue;
            }

            shortVideos.Add(
                new ShortVideo
                {
                    Id = id,
                    VideoId = !string.IsNullOrWhiteSpace(raw.VideoId) ? raw.VideoId : id,
                    Title = raw.Title,
                    Description = raw.Description,
                    CoverUrl = raw.CoverUrl,
                }
            );
        }

        return shortVideos;
    }

    private async Task<AppBookShelfListData> FetchBookShelfAsync(
        CancellationToken cancellationToken
    )
    {
        AppResponse<AppBookShelfListData> response = await _appTransport
            .GetAsync<AppBookShelfListData>(
                "/reading/bookapi/bookshelf/list/v",
                [
                    new(
                        "server_time",
                        DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)
                    ),
                ],
                cancellationToken
            )
            .ConfigureAwait(false);
        return response.Data ?? new();
    }

    private async Task<IReadOnlyList<AppShelfItem>> FetchVideoShelfAsync(
        CancellationToken cancellationToken
    )
    {
        AppResponse<AppShelfListData> response = await _appTransport
            .GetAsync<AppShelfListData>(
                "/reading/bookapi/bookshelf/video/list/v",
                [],
                cancellationToken
            )
            .ConfigureAwait(false);
        AppShelfListData data = response.Data ?? new();
        IReadOnlyList<AppShelfItem> shelfItems = data.ShelfItems ?? [];
        IReadOnlyList<AppShelfItem> listItems = data.Items ?? [];
        return shelfItems.Count > 0 ? shelfItems : listItems;
    }

    internal async Task<PostFavoritePage> GetPostFavoritesAsync(
        int count,
        int offset,
        CancellationToken cancellationToken
    )
    {
        ValidatePage(count, offset);
        AppResponse<AppPostFavoriteListData> response = await _appTransport
            .GetAsync<AppPostFavoriteListData>(
                "/reading/ugc/booklist/get/v",
                [
                    new("book_list_types", "6"),
                    new("count", count.ToString(CultureInfo.InvariantCulture)),
                    new("offset", offset.ToString(CultureInfo.InvariantCulture)),
                    new("show_update_notify", "false"),
                ],
                cancellationToken
            )
            .ConfigureAwait(false);
        AppPostFavoriteListData data = response.Data ?? new();
        List<Post> items = [];
        foreach (AppTopicBooklistItem entry in data.Items ?? [])
        {
            if (entry.PostData is not null)
            {
                Post mapped = SearchService.MapSearchPost(entry.PostData);
                if (mapped.Id.Length > 0)
                {
                    items.Add(mapped);
                }
            }
        }

        return new PostFavoritePage
        {
            Items = items,
            NextOffset = data.NextOffset,
            HasMore = data.HasMore,
        };
    }

    private static IReadOnlyList<string> ParseTags(string tags) =>
        string.IsNullOrWhiteSpace(tags)
            ? []
            : [.. tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
}
