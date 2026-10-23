namespace PhoenixReadApi.Internal.Infrastructure.Http;

/// <summary>按大小限制读取响应正文，避免缓存超大或不受信任的数据。</summary>
internal static class ResponseReader
{
    internal static async Task<byte[]> ReadBytesAsync(
        HttpResponseMessage response,
        int maxBytes,
        CancellationToken cancellationToken
    )
    {
        await using Stream stream = await response
            .Content.ReadAsStreamAsync(cancellationToken)
            .ConfigureAwait(false);
        using MemoryStream buffer = new();
        byte[] chunk = new byte[16 * 1024];
        while (true)
        {
            int read = await stream.ReadAsync(chunk, cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                break;
            }

            if (buffer.Length + read > maxBytes)
            {
                throw new PhoenixReadApiResponseException(
                    "站点响应过大.",
                    response.RequestMessage?.RequestUri?.AbsolutePath
                );
            }

            buffer.Write(chunk, 0, read);
        }

        return buffer.ToArray();
    }
}
