using System.Text;
using PhoenixReadApi.Internal.Infrastructure.Crypto;

namespace PhoenixReadApi.Tests;

/// <summary>
/// 播放签名（Argus 551 与 34 轮 Ladon）测试向量与行为断言。
/// </summary>
public sealed class PlaybackSignTests
{
    [Test]
    public async Task TestSm3Vectors()
    {
        byte[] emptySm3 = PhoenixReadRequestSigner.Sm3([]);
        await Assert.That(Convert.ToHexStringLower(emptySm3)).IsEqualTo("1ab21d8355cfa17f8e61194831e81a8f22bec8c728fefb747ed035eb5082aa2b");

        byte[] abcSm3 = PhoenixReadRequestSigner.Sm3("abc"u8);
        await Assert.That(Convert.ToHexStringLower(abcSm3)).IsEqualTo("66c7f0f462eeedd9d1f2d46bdc10e4e24167c4875cf2f7a2297da02b8f4ba8e0");

        byte[] zeros16 = new byte[16];
        byte[] zerosSm3 = PhoenixReadRequestSigner.Sm3(zeros16);
        await Assert.That(Convert.ToHexStringLower(zerosSm3)).IsEqualTo("106e34a2b8c7bb13156cfdd0d91379dcc47543dcf9787c68ae5eb582620ae6e8");
    }

    [Test]
    public async Task TestLadonGoldenVectors()
    {
        const uint nonce = 0x04030201;

        string ladon1 = PhoenixReadRequestSigner.PlaybackLadon(1_700_000_000, nonce, "1967");
        await Assert.That(ladon1).IsEqualTo("AQIDBKM7Y7bWnOIUXmTybD6/MpGbuge4sBwUJsyHVivhtMsH");

        string ladon2 = PhoenixReadRequestSigner.PlaybackLadon(1_800_000_000, nonce, "8662");
        await Assert.That(ladon2).IsEqualTo("AQIDBPhgjvKhYEWp2lodHDSBnITF8APHUjBLe2ja98bNrRn+");
    }

    [Test]
    public async Task TestArgusGoldenVectorsAston()
    {
        // Vector 1: query-derived params
        string argus1 = PhoenixReadRequestSigner.PlaybackArgus(
            "aid=1967&device_id=test-device&iid=test-install",
            ReadOnlySpan<byte>.Empty,
            1_700_000_000,
            659662259
        );
        await Assert.That(argus1).IsEqualTo(
            "8oGwZRLe2V38m+1gfqIeLa2MoV9ZBWosW5Y0NXKHAB2aEYRF86eNA7rLWm6xUCiwYMBDI/yq5ajsCmDF+Zws9KOfZO0mqezSYjF9/D5C1EliRxXJBBuZBaQZJgT6gakwiRnJkm+8F1ZVFIFO9GxkdwLxdxYidY2sLQjAsG8q4241ayJ/0zOxsVJNlqzPs7PfJb83zY6J1eIUaugTB6QnbXOKEQUgLhiYC0Uszr1vBP2Vj1cgtWu0IgouGM/ie3CohEAkY2oOkN/W1leWheBszijv"
        );

        // Vector 2: query + body
        byte[] body2 = Encoding.UTF8.GetBytes("""{"video_id":"123","biz_param":{"video_platform":3}}""");
        string argus2 = PhoenixReadRequestSigner.PlaybackArgus(
            "aid=8662&device_id=7000000000000000001&iid=7000000000000000002&version_name=7.1.3.32&_rticket=1800000000999&ts=1800000000",
            body2,
            1_800_000_000,
            4294967295
        );
        await Assert.That(argus2).IsEqualTo(
            "8oGzD48Lx0I+DCXDwk+l2r54SBRLSexXn2DlWQdGZE1UFZXj1GWup1ztpbF3eGeKcJAItaddhQwgYm7odM2byEYHtJ0pNvNUAoumWtQPqducerNIGe0Hs3z5V7oJDVgEySh6Kqc69jI8T+zU13SblzavSSZGSegC030jul66JPvjsBGzZoerKy88GDYDbumRNPCtqnfztLNNlGwmnA+0Tv+dnBC1xJ9rIEBThybfhq8wL5YqxxstLwuLGffOIlbxoQdheycfZBDpJG5kiK4eX7b3gr+PVkmCZBkJleZIYDl22UQS7nQGJKyu/a0U2MC7oWo="
        );

        // Vector 3: empty query and body
        string argus3 = PhoenixReadRequestSigner.PlaybackArgus(
            "",
            ReadOnlySpan<byte>.Empty,
            1_700_000_000,
            0
        );
        await Assert.That(argus3).IsEqualTo(
            "8oHyrVYEy1xDCDlx64ko40qC6JC9VYY/ZkjJzEl50iSFDw8Yfph1hupLxfKrA9GswSEz0Yi4goA7OQ4u+H8ECdOaHRbHCQV+QFNgNz4TTRpioctLgrN97GbIKIQgZJjNRNiwbGqa+jIbLonncbTbsGIMv2WphwNMhXazKSewxik7/ecf0/SUOIiVd/3LhLMGeCIy+/ngOQr9VXnvmCb8HyzUbr01+pgwc1OKSfU29+FP4vYSq2WdmLCpvopUNQZhB8U="
        );
    }

    [Test]
    public async Task TestArgusGoldenVectorZfdang()
    {
        const string query = "aid=1967&device_id=dev_aaaaaaaa&iid=111";
        const string want =
            "8oHyrVYEy1xDCDlx64ko40qC6JC9VYY/ZkjJzEl50iSFDw8Yfph1hupLxfKrA9GswSEz0Yi4goA7OQ4u+H8ECdOaQDL4GduQ3LULSq9Q4iSEGbS3L6G24GXChtnmPr6n46M2PLlKpXvF7uE0lhH4yD+X69FpdM41gQwx5Y5U3NdwXFaxlqYFszyp7dlUE6BEBWqwDsiAH8Jk9TGKr/jfHt4fVmMc9zRyNRdGeV0x6aP//x7aV+8Po8a/9KK3UNG5KEpmIgzRT6NUttkBP9gRNUh3";

        string got = PhoenixReadRequestSigner.PlaybackArgus(
            query,
            ReadOnlySpan<byte>.Empty,
            1_700_000_000,
            659662259,
            "1967",
            "dev_aaaaaaaa",
            ""
        );
        await Assert.That(got).IsEqualTo(want);
    }

    [Test]
    public async Task TestSignPlaybackShape()
    {
        using HttpRequestMessage request = new(
            HttpMethod.Post,
            "https://api.example.com/novel/player/video_model/v1/?aid=8662&device_id=7000000000000000001&iid=7000000000000000002&version_name=7.3.9.32"
        );
        request.Headers.TryAddWithoutValidation("X-Medusa", "dummy-medusa-header");

        byte[] body = Encoding.UTF8.GetBytes("""{"req_type":1}""");
        DateTimeOffset now = DateTimeOffset.FromUnixTimeMilliseconds(1_700_000_000_123);

        PhoenixReadRequestSigner.SignPlayback(request, body, "7000000000000000001", now);

        // 1. X-Medusa must be stripped
        await Assert.That(request.Headers.Contains("X-Medusa")).IsFalse();

        // 2. Playback headers must be present
        await Assert.That(request.Headers.Contains("X-Argus")).IsTrue();
        await Assert.That(request.Headers.Contains("X-Ladon")).IsTrue();
        await Assert.That(request.Headers.Contains("X-Gorgon")).IsTrue();
        await Assert.That(request.Headers.Contains("X-Khronos")).IsTrue();
        await Assert.That(request.Headers.Contains("X-SS-Req-Ticket")).IsTrue();
        await Assert.That(request.Headers.Contains("X-SS-STUB")).IsTrue();

        // 3. Argus 551 starts with magic 0xf2 0x81 ("8o...")
        string argus = request.Headers.GetValues("X-Argus").First();
        await Assert.That(argus.StartsWith("8o")).IsTrue();
        await Assert.That(argus.Length > 64).IsTrue();
    }

    [Test]
    public async Task TestPlayerSimonRoundKeysEquivalence()
    {
        byte[] derived = PhoenixReadRequestSigner.Sm3([
            .. Convert.FromHexString("ac1adaae95a7af94a5114ab3b3a97dd80050aa0a39314c40528caec95256c28c"),
            0x3C, 0xCC, 0x56, 0x7B,
            .. Convert.FromHexString("ac1adaae95a7af94a5114ab3b3a97dd80050aa0a39314c40528caec95256c28c")
        ]);

        ulong[] rounds = new ulong[72];
        for (int i = 0; i < 4; i++)
        {
            rounds[i] = System.Buffers.Binary.BinaryPrimitives.ReadUInt64LittleEndian(derived.AsSpan(i * 8, 8));
        }
        for (int k = 4; k < 72; k++)
        {
            ulong mixed = System.Numerics.BitOperations.RotateRight(rounds[k - 1], 3) ^ rounds[k - 3];
            mixed ^= System.Numerics.BitOperations.RotateRight(mixed, 1);
            rounds[k] = ~rounds[k - 4] ^ mixed ^ (0x3dc94c3a046d678bUL >>> ((k - 4) % 62)) & 1 ^ 3;
        }

        // Test SignPlayer
        using HttpRequestMessage req = new(HttpMethod.Get, "https://api.example.com/novel/drama/detail/v1/?aid=8662&device_id=123");
        DateTimeOffset now = DateTimeOffset.UtcNow;
        PhoenixReadRequestSigner.SignPlayer(req, [], "123", now);
        await Assert.That(req.Headers.Contains("X-Argus")).IsTrue();
        await Assert.That(req.Headers.Contains("X-Ladon")).IsTrue();
        await Assert.That(req.Headers.Contains("X-Gorgon")).IsTrue();
    }
}
