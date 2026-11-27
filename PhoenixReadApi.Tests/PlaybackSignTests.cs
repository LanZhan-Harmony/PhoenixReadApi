using System.Text;
using System.Text.Json;
using PhoenixReadApi.Internal.Contracts.App;
using PhoenixReadApi.Internal.Contracts.App.Playback;
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
        await Assert
            .That(Convert.ToHexStringLower(emptySm3))
            .IsEqualTo("1ab21d8355cfa17f8e61194831e81a8f22bec8c728fefb747ed035eb5082aa2b");

        byte[] abcSm3 = PhoenixReadRequestSigner.Sm3("abc"u8);
        await Assert
            .That(Convert.ToHexStringLower(abcSm3))
            .IsEqualTo("66c7f0f462eeedd9d1f2d46bdc10e4e24167c4875cf2f7a2297da02b8f4ba8e0");

        byte[] zeros16 = new byte[16];
        byte[] zerosSm3 = PhoenixReadRequestSigner.Sm3(zeros16);
        await Assert
            .That(Convert.ToHexStringLower(zerosSm3))
            .IsEqualTo("106e34a2b8c7bb13156cfdd0d91379dcc47543dcf9787c68ae5eb582620ae6e8");
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
        await Assert
            .That(argus1)
            .IsEqualTo(
                "8oGwZRLe2V38m+1gfqIeLa2MoV9ZBWosW5Y0NXKHAB2aEYRF86eNA7rLWm6xUCiwYMBDI/yq5ajsCmDF+Zws9KOfZO0mqezSYjF9/D5C1EliRxXJBBuZBaQZJgT6gakwiRnJkm+8F1ZVFIFO9GxkdwLxdxYidY2sLQjAsG8q4241ayJ/0zOxsVJNlqzPs7PfJb83zY6J1eIUaugTB6QnbXOKEQUgLhiYC0Uszr1vBP2Vj1cgtWu0IgouGM/ie3CohEAkY2oOkN/W1leWheBszijv"
            );

        // Vector 2: query + body
        byte[] body2 = Encoding.UTF8.GetBytes(
            """{"video_id":"123","biz_param":{"video_platform":3}}"""
        );
        string argus2 = PhoenixReadRequestSigner.PlaybackArgus(
            "aid=8662&device_id=7000000000000000001&iid=7000000000000000002&version_name=7.1.3.32&_rticket=1800000000999&ts=1800000000",
            body2,
            1_800_000_000,
            4294967295
        );
        await Assert
            .That(argus2)
            .IsEqualTo(
                "8oGzD48Lx0I+DCXDwk+l2r54SBRLSexXn2DlWQdGZE1UFZXj1GWup1ztpbF3eGeKcJAItaddhQwgYm7odM2byEYHtJ0pNvNUAoumWtQPqducerNIGe0Hs3z5V7oJDVgEySh6Kqc69jI8T+zU13SblzavSSZGSegC030jul66JPvjsBGzZoerKy88GDYDbumRNPCtqnfztLNNlGwmnA+0Tv+dnBC1xJ9rIEBThybfhq8wL5YqxxstLwuLGffOIlbxoQdheycfZBDpJG5kiK4eX7b3gr+PVkmCZBkJleZIYDl22UQS7nQGJKyu/a0U2MC7oWo="
            );

        // Vector 3: empty query and body
        string argus3 = PhoenixReadRequestSigner.PlaybackArgus(
            "",
            ReadOnlySpan<byte>.Empty,
            1_700_000_000,
            0
        );
        await Assert
            .That(argus3)
            .IsEqualTo(
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
    public async Task TestGorgonGoldenVector()
    {
        const string query =
            "iid=7100000000000012&device_id=7100000000000011&ac=wifi&channel=update_64"
            + "&aid=8662&app_name=novelread&version_code=71332&version_name=7.1.3.32"
            + "&device_platform=android&os=android&ssmix=a&device_type=25053RT47C"
            + "&device_brand=Redmi&language=zh&os_api=36&os_version=16"
            + "&manifest_version_code=71332&resolution=1280*2772&dpi=520"
            + "&update_version_code=71332&host_abi=arm64-v8a&dragon_device_type=phone"
            + "&pv_player=71332&compliance_status=0&need_personal_recommend=1"
            + "&player_so_load=1&is_android_pad_screen=0"
            + "&ts=1700000000&_rticket=1700000000123";
        string got = PhoenixReadRequestSigner.PlaybackGorgon(
            query,
            """{"a":1}"""u8,
            1_700_000_000,
            0x1234
        );
        await Assert.That(got).IsEqualTo("84043412400154ed198c3230d5ff1364e71df6105a46126993c0");
    }

    [Test]
    public async Task TestHeliosGoldenVector()
    {
        string got = PhoenixReadRequestSigner.PlaybackHelios(1_700_000_000, 2_028_160_147);
        await Assert.That(got).IsEqualTo("k0TjeBWFFnRXfkeKlh3Hmx1jLjrfhgCasqsF4XvB1+0eB1Mw");
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
            .. Convert.FromHexString(
                "ac1adaae95a7af94a5114ab3b3a97dd80050aa0a39314c40528caec95256c28c"
            ),
            0x3C,
            0xCC,
            0x56,
            0x7B,
            .. Convert.FromHexString(
                "ac1adaae95a7af94a5114ab3b3a97dd80050aa0a39314c40528caec95256c28c"
            ),
        ]);

        ulong[] rounds = new ulong[72];
        for (int i = 0; i < 4; i++)
        {
            rounds[i] = System.Buffers.Binary.BinaryPrimitives.ReadUInt64LittleEndian(
                derived.AsSpan(i * 8, 8)
            );
        }
        for (int k = 4; k < 72; k++)
        {
            ulong mixed =
                System.Numerics.BitOperations.RotateRight(rounds[k - 1], 3) ^ rounds[k - 3];
            mixed ^= System.Numerics.BitOperations.RotateRight(mixed, 1);
            rounds[k] = ~rounds[k - 4] ^ mixed ^ (0x3dc94c3a046d678bUL >>> ((k - 4) % 62)) & 1 ^ 3;
        }

        // Test SignPlayer
        using HttpRequestMessage req = new(
            HttpMethod.Get,
            "https://api.example.com/novel/drama/detail/v1/?aid=8662&device_id=123"
        );
        DateTimeOffset now = DateTimeOffset.UtcNow;
        PhoenixReadRequestSigner.SignPlayer(req, [], "123", now);
        await Assert.That(req.Headers.Contains("X-Argus")).IsTrue();
        await Assert.That(req.Headers.Contains("X-Ladon")).IsTrue();
        await Assert.That(req.Headers.Contains("X-Gorgon")).IsTrue();
    }

    [Test]
    public async Task TestV5CarrierVectors()
    {
        string path = @"D:\code\Python\hongguo\_gh_v5\signing-v5-carrier.json";
        if (!File.Exists(path))
        {
            return;
        }

        string json = await File.ReadAllTextAsync(path);
        using System.Text.Json.JsonDocument doc = System.Text.Json.JsonDocument.Parse(json);
        foreach (JsonElement elem in doc.RootElement.EnumerateArray())
        {
            uint ts = elem.GetProperty("ts").GetUInt32();
            byte[] source = Convert.FromHexString(elem.GetProperty("source").GetString()!);
            byte[] key = Convert.FromHexString(elem.GetProperty("key").GetString()!);
            byte[] iv = Convert.FromHexString(elem.GetProperty("iv").GetString()!);
            string expectedEnc = elem.GetProperty("encrypted").GetString()!;

            byte[] encrypted = PhoenixReadPlaybackSigner.AesV3Encrypt(source, key, iv, ts);
            await Assert
                .That(Convert.ToHexStringLower(encrypted))
                .IsEqualTo(expectedEnc.ToLowerInvariant());
        }
    }

    [Test]
    public async Task TestV5F13Vectors()
    {
        string path = @"D:\code\Python\hongguo\_gh_v5\signing-v5-f13.json";
        if (!File.Exists(path))
        {
            return;
        }

        string json = await File.ReadAllTextAsync(path);
        using System.Text.Json.JsonDocument doc = System.Text.Json.JsonDocument.Parse(json);
        foreach (JsonElement elem in doc.RootElement.EnumerateArray())
        {
            uint ts = elem.GetProperty("ts").GetUInt32();
            byte[] query = Convert.FromHexString(elem.GetProperty("query").GetString()!);
            byte[] body = Convert.FromHexString(elem.GetProperty("body").GetString()!);
            string expectedDigest = elem.GetProperty("digest").GetString()!;

            byte[] f13 = PhoenixReadPlaybackSigner.HashF13V5(query, body, ts);
            // The golden vector digest in signing-v5-f13.json is the 16-byte folded part
            await Assert
                .That(Convert.ToHexStringLower(f13[..16]))
                .IsEqualTo(expectedDigest.ToLowerInvariant());
        }
    }

    [Test]
    public async Task TestV5PayloadAndLive()
    {
        string url =
            "https://api5-normal-sinfonlinea.fqnovel.com/novel/player/multi_video_model/player/v1?aid=8662&app_name=novelread&version_code=74332&version_name=7.4.3.32&channel=vivo_8662_64&device_platform=android&device_type=PGT-AN10&os_version=12&update_version_code=74332&iid=1483721089225898&cdid=test-cdid&device_id=1483721089221802&_rticket=1794887545022";
        byte[] body = Convert.FromHexString("1f8b0800000000000000558fd14ec3300c45ffc5");
        uint ts = 1794887545;
        string deviceId = "1483721089221802";
        string nonceB64 = "AAAAEBAQEBAQEBAQEBAQEBAQ";
        long startMs = 1794887000000;
        uint msgRandom = 123456789;
        uint nonce = 0x11223344;
        uint packRand = 0x55667788;

        string csharpMedusa = PhoenixReadPlaybackSigner.GenerateMedusaCore(
            url,
            body,
            ts,
            deviceId,
            nonceB64,
            startMs,
            msgRandom,
            nonce,
            packRand,
            osVersion: "12",
            deviceType: "PGT-AN10"
        );

        await Assert.That(csharpMedusa).IsNotEmpty();
        await Assert.That(csharpMedusa.Length).IsGreaterThan(800);

        PhoenixReadApi.Internal.Contracts.App.Playback.AppPlaybackPayload payload = new()
        {
            BizParam = new PhoenixReadApi.Internal.Contracts.App.Playback.AppPlaybackBizParam
            {
                DetailPageVersion = 0,
                DeviceLevel = 1,
                DisableDiggStat = false,
                DisableVideoRelateBook = false,
                NeedAllVideoDefinition = true,
                NeedMp4Align = false,
                UseOsPlayer = false,
                UseServerDns = false,
                VideoPlatform = 1024,
            },
            MixedVideoIdMap = new Dictionary<string, List<string>>
            {
                ["1004"] = ["7484637255605160985"],
            },
            DrScene = "player",
        };

        byte[] jsonBytes =
            PhoenixReadApi.Internal.Serialization.JsonAotSerializer.SerializeToUtf8Bytes(payload);
        byte[] gzipBody = PhoenixReadPlaybackSigner.CompressJavaGzip(jsonBytes);

        using HttpClient client = new(
            new HttpClientHandler { AutomaticDecompression = System.Net.DecompressionMethods.All }
        );

        using HttpResponseMessage probe = await client.GetAsync(
            "https://api5-normal-sinfonlinea.fqnovel.com/"
        );
        DateTimeOffset? serverDate = probe.Headers.Date;
        long serverMs = serverDate?.ToUnixTimeMilliseconds()
            ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        long baseSec = serverMs / 1000;

        string[] candidateHosts =
        [
            "https://api5-normal-sinfonlinea.fqnovel.com/novel/player/video_model/v1/",
            "https://api5-normal-sinfonlinea.fqnovel.com/novel/player/multi_video_model/player/v1",
            "https://api5-normal-sinfonlinea.fqnovel.com/novel/player/multi_video_model/v1",
        ];
        string installId = "1483721089225898";
        string cdid = "test-cdid";

        byte[]? successBytes = null;
        string? lastErr = null;
        foreach (string targetHost in candidateHosts)
        {
            bool isSingle = targetHost.Contains("/video_model/v1/");
            byte[] bodyBytes = isSingle
                ? PhoenixReadApi.Internal.Serialization.JsonAotSerializer.SerializeToUtf8Bytes(
                    new PhoenixReadApi.Internal.Contracts.App.Playback.AppSinglePlaybackPayload
                    {
                        VideoId = "7484637255605160985",
                        BizParam = new PhoenixReadApi.Internal.Contracts.App.Playback.AppSinglePlaybackBizParam
                        {
                            NeedAllVideoDefinition = true,
                            VideoIdType = 0,
                            VideoPlatform = 3,
                        },
                    }
                )
                : gzipBody;
            byte[] bodyMd5 = System.Security.Cryptography.MD5.HashData(bodyBytes);

            (string prefix, string suffix) = PhoenixReadPlaybackSigner.BuildPlaybackV5UrlParts(
                targetHost,
                deviceId,
                installId,
                cdid
            );
            long ticket = PhoenixReadPlaybackSigner.FindTicketMsForBranch12(
                prefix,
                suffix,
                bodyMd5,
                (uint)baseSec,
                baseSec * 1000
            );
            string candUrl = $"{prefix}{ticket}{suffix}";
            (string? khronos, string? gorgon, string? helios, string? medusa, string? argus, string? ladon, string? ssStub) =
                PhoenixReadPlaybackSigner.SignV5(candUrl, bodyBytes, (uint)baseSec, deviceId, installId);

            using HttpRequestMessage req = new(HttpMethod.Post, candUrl);
            req.Headers.TryAddWithoutValidation(
                "User-Agent",
                PhoenixReadApi
                    .Internal
                    .Infrastructure
                    .Runtime
                    .PhoenixReadDeviceProfile
                    .PlaybackUserAgent
            );
            req.Headers.TryAddWithoutValidation("Accept", "application/json");
            req.Headers.TryAddWithoutValidation("x-ss-stub", ssStub);
            req.Headers.TryAddWithoutValidation("x-khronos", khronos);
            req.Headers.TryAddWithoutValidation("x-gorgon", gorgon);
            req.Headers.TryAddWithoutValidation("x-helios", helios);
            req.Headers.TryAddWithoutValidation("x-medusa", medusa);
            req.Headers.TryAddWithoutValidation("x-argus", argus);
            req.Headers.TryAddWithoutValidation("x-ladon", ladon);

            req.Content = new ByteArrayContent(bodyBytes);
            req.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
                "application/json"
            )
            {
                CharSet = "utf-8",
            };
            if (!isSingle)
            {
                req.Content.Headers.ContentEncoding.Add("gzip");
            }

            using HttpResponseMessage res = await client.SendAsync(req);
            byte[] bytes = await res.Content.ReadAsByteArrayAsync();
            if (bytes.Length > 1000)
            {
                successBytes = bytes;
                break;
            }
            lastErr =
                $"HTTP {(int)res.StatusCode}: {Encoding.UTF8.GetString(bytes)} (len={bytes.Length}) for {candUrl}";
        }

        if (successBytes is null)
        {
            const string liveVideoId = "7484637255605160985";
            Span<byte> zeroMd5 = stackalloc byte[16];
            string tabPrefix = PhoenixReadPlaybackSigner.BuildVideoTabUrlPrefix(
                "https://api5-normal-sinfonlinea.fqnovel.com/reading/bookapi/video_tab/multi_video_model/v1/",
                liveVideoId,
                deviceId,
                installId,
                cdid
            );
            long ticket = PhoenixReadPlaybackSigner.FindTicketMsForBranch12(
                tabPrefix,
                string.Empty,
                zeroMd5,
                (uint)baseSec,
                baseSec * 1000
            );
            string tabUrl = $"{tabPrefix}{ticket}";
            string khronos = XKhronosSigner.Format((uint)baseSec);
            string helios = XHeliosSigner.SignV5((uint)baseSec);
            string medusa = XMedusaSigner.SignV5(tabUrl, [], (uint)baseSec, deviceId);

            using HttpRequestMessage req = new(HttpMethod.Get, tabUrl);
            req.Headers.TryAddWithoutValidation(
                "User-Agent",
                PhoenixReadApi.Internal.Infrastructure.Runtime.PhoenixReadDeviceProfile.PlaybackUserAgent
            );
            req.Headers.TryAddWithoutValidation("Accept", "application/json");
            req.Headers.TryAddWithoutValidation("x-khronos", khronos);
            req.Headers.TryAddWithoutValidation("x-helios", helios);
            req.Headers.TryAddWithoutValidation("x-medusa", medusa);

            using HttpResponseMessage res = await client.SendAsync(req);
            byte[] bytes = await res.Content.ReadAsByteArrayAsync();
            if (bytes.Length > 1000)
            {
                successBytes = bytes;
            }
            else
            {
                lastErr += $" | VideoTab len={bytes.Length}";
            }
        }

        if (successBytes is null)
        {
            throw new Exception("Live probe failed: " + lastErr);
        }

        await Assert.That(successBytes).IsNotNull();
        await Assert.That(successBytes!.Length > 1000).IsTrue();

        AppResponse<AppPlaybackData>? appResp =
            PhoenixReadApi.Internal.Serialization.JsonAotSerializer.Deserialize<PhoenixReadApi.Internal.Contracts.App.AppResponse<PhoenixReadApi.Internal.Contracts.App.Playback.AppPlaybackData>>(
                successBytes!
            );
        await Assert.That(appResp).IsNotNull();
        await Assert.That(appResp!.Data).IsNotNull();
        // Check if video_model is in Group1004 or AdditionalData
        bool hasModel =
            (appResp.Data!.Group1004 is { Count: > 0 })
            || (appResp.Data.VideoModelData is { Count: > 0 })
            || (appResp.Data.AdditionalData is { Count: > 0 })
            || appResp.Data.VideoModel.Variants.Count > 0
            || appResp.Data.VideoList.Count > 0;
        await Assert.That(hasModel).IsTrue();
    }
}
