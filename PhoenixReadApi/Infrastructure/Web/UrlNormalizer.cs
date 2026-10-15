using System.Net;

namespace PhoenixReadApi.Internal.Infrastructure.Web;

/// <summary>在网页接口层规范化并校验媒体 URL。</summary>
internal static class UrlNormalizer
{
    internal static string NormalizeMediaUrl(string? raw, Uri page)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return string.Empty;
        }

        var value = WebUtility.HtmlDecode(raw.Trim().Trim('"', '\''));
        value = value.Replace("\\/", "/", StringComparison.Ordinal);
        if (value.StartsWith("//", StringComparison.Ordinal))
        {
            value = "https:" + value;
        }

        return Uri.TryCreate(page, value, out Uri? resolved) && IsMediaUrl(resolved)
            ? resolved.ToString()
            : string.Empty;
    }

    internal static bool IsHttpUrl(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out Uri? uri)
        && uri is { IsAbsoluteUri: true, UserInfo: "" }
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
        && !string.IsNullOrWhiteSpace(uri.Host)
        && value!.Length <= 8192;

    internal static bool IsMediaUrl(Uri uri) =>
        IsHttpUrl(uri.ToString())
        && (
            uri.AbsolutePath.Contains(".m3u8", StringComparison.OrdinalIgnoreCase)
            || uri.AbsolutePath.Contains(".mp4", StringComparison.OrdinalIgnoreCase)
        );
}
