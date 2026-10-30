using System.Buffers;

namespace PhoenixReadApi.Internal.Infrastructure.Http;

/// <summary>按大小限制读取响应正文，避免缓存超大或不受信任的数据。</summary>
internal static class ResponseReader
{
    private const int ChunkSize = 16 * 1024;

    internal static async Task<byte[]> ReadBytesAsync(
        HttpResponseMessage response,
        int maxBytes,
        CancellationToken cancellationToken
    )
    {
        await using Stream stream = await response
            .Content.ReadAsStreamAsync(cancellationToken)
            .ConfigureAwait(false);

        long? contentLength = response.Content.Headers.ContentLength;
        int initialCapacity =
            contentLength is > 0 && contentLength.Value <= maxBytes ? (int)contentLength.Value : 0;

        using MemoryStream buffer =
            initialCapacity > 0 ? new MemoryStream(initialCapacity) : new MemoryStream();
        byte[] chunk = ArrayPool<byte>.Shared.Rent(ChunkSize);
        try
        {
            while (true)
            {
                int read = await stream
                    .ReadAsync(chunk.AsMemory(0, ChunkSize), cancellationToken)
                    .ConfigureAwait(false);
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
        finally
        {
            ArrayPool<byte>.Shared.Return(chunk);
        }
    }
}
