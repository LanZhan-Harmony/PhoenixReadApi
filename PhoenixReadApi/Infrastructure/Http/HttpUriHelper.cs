namespace PhoenixReadApi.Internal.Infrastructure.Http;

/// <summary>HTTP URI 构建和查询参数拼接辅助类。</summary>
internal static class HttpUriHelper
{
    internal static Uri CreateRawUri(
        Uri baseUri,
        string path,
        IEnumerable<KeyValuePair<string, string>> query
    )
    {
        string queryText = string.Join(
            '&',
            query.Select(static pair =>
                EncodeQueryComponent(pair.Key) + "=" + EncodeQueryComponent(pair.Value)
            )
        );
        return new Uri(
            baseUri.AbsoluteUri.TrimEnd('/') + "/" + path.TrimStart('/') + "?" + queryText,
            UriKind.Absolute
        );

        static string EncodeQueryComponent(string value) =>
            Uri.EscapeDataString(value).Replace("%2A", "*", StringComparison.OrdinalIgnoreCase);
    }

    internal static Uri AddQuery(Uri uri, string key, string value)
    {
        string query = uri.Query.TrimStart('?');
        string joined = query.Length == 0 ? string.Empty : query + "&";
        joined += Uri.EscapeDataString(key) + "=" + Uri.EscapeDataString(value);
        return new UriBuilder(uri) { Query = joined }.Uri;
    }
}
