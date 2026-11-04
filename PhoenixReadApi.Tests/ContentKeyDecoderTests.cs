namespace PhoenixReadApi.Tests;

/// <summary>
/// CENC 媒体密钥解码与 Spade 算法测试。
/// </summary>
public sealed class ContentKeyDecoderTests
{
    [Test]
    public async Task TestContentKeyDecoderV2MatchesLiveSpade()
    {
        // 验证对 app_v2/web_v2 媒体密钥的逆向解码
        const string liveSpade = "nLwSxG6JI85zhT//c7AK4HauO+lzrg7TdZwO0XWrP+J0pDypqQ==";
        byte[] keyBytes = PhoenixReadApi.Internal.Infrastructure.Crypto.ContentKeyDecoder.Decode(liveSpade);
        string hexKey = Convert.ToHexStringLower(keyBytes);
        await Assert.That(hexKey).IsEqualTo("1be760855c48a8c86000eeb8c5bb1010");
    }

    [Test]
    public async Task TestContentKeyDecoderRejectsGarbage()
    {
        await Assert.ThrowsAsync<PhoenixReadApiResponseException>(async () =>
        {
            PhoenixReadApi.Internal.Infrastructure.Crypto.ContentKeyDecoder.Decode(string.Empty);
            await Task.CompletedTask;
        });

        await Assert.ThrowsAsync<PhoenixReadApiResponseException>(async () =>
        {
            PhoenixReadApi.Internal.Infrastructure.Crypto.ContentKeyDecoder.Decode("not base64 !!");
            await Task.CompletedTask;
        });

        await Assert.ThrowsAsync<PhoenixReadApiResponseException>(async () =>
        {
            PhoenixReadApi.Internal.Infrastructure.Crypto.ContentKeyDecoder.Decode("AAAA");
            await Task.CompletedTask;
        });
    }
}
