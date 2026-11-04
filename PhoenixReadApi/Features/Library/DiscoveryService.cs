using System.Globalization;
using PhoenixReadApi.Internal.Contracts.App;
using PhoenixReadApi.Internal.Contracts.App.Library;
using PhoenixReadApi.Internal.Infrastructure.Runtime;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Features.Library;

/// <summary>分享链接生成与解析。</summary>
internal sealed class DiscoveryService : LibraryServiceBase
{
    internal DiscoveryService(PhoenixReadClientRuntime runtime)
        : base(runtime) { }

    internal async Task<ShareLink> CreateShareLinkAsync(
        string seriesId,
        string videoId,
        CancellationToken cancellationToken
    )
    {
        seriesId = NormalizeSeriesId(seriesId);
        videoId = NormalizeVideoId(videoId);
        string fallbackUrl =
            videoId.Length > 0
                ? $"https://fanqienovel.com/drama/{seriesId}?video_id={videoId}"
                : $"https://fanqienovel.com/drama/{seriesId}";
        AppResponse<AppShareData>? response = null;
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
        catch (PhoenixReadApiHttpException exception) when (exception.StatusCode == 404) { }
        AppShareData data = response?.Data ?? new();
        string url = ValueNormalizer.First(data.Url, data.ShortUrl, fallbackUrl);
        string shortUrl = ValueNormalizer.First(data.ShortUrl, data.Url, fallbackUrl);
        return new ShareLink
        {
            SeriesId = seriesId,
            VideoId = videoId,
            Url = url,
            ShortUrl = shortUrl,
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
            SeriesId = ValueNormalizer.IsNumericId(seriesId) ? seriesId : string.Empty,
            VideoId = ValueNormalizer.IsNumericId(videoId) ? videoId : string.Empty,
        };
    }
}
