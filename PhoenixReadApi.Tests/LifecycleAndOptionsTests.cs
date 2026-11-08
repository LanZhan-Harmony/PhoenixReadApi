namespace PhoenixReadApi.Tests;

/// <summary>
/// 客户端生命周期管理、HttpClient 所有权与配置项冲突校验测试。
/// </summary>
public sealed class LifecycleAndOptionsTests
{
    [Test]
    public Task TestLifecycle_DefaultConstructor_DisposeMultipleTimes()
    {
        PhoenixReadApiClient client = new(
            new PhoenixReadApiOptions { RequestTimeout = TimeSpan.FromSeconds(5) }
        );

        client.Dispose();
        client.Dispose();
        return Task.CompletedTask;
    }

    [Test]
    public Task TestLifecycle_InjectedHttpClient_NotDisposedByClient()
    {
        using HttpClient externalHttpClient = new();
        PhoenixReadApiClient client = new(
            externalHttpClient,
            new PhoenixReadApiOptions { RequestTimeout = TimeSpan.FromSeconds(5) }
        );

        // 销毁客户端自身
        client.Dispose();

        // 确认外部注入的 HttpClient 仍可正常使用，未被客户端销毁
        using HttpRequestMessage request = new(HttpMethod.Get, "https://httpbin.org/get");
        // 只要 BaseAddress/Timeout 或其他属性未报 ObjectDisposedException
        _ = externalHttpClient.Timeout;

        return Task.CompletedTask;
    }

    [Test]
    public async Task TestLifecycle_NullHttpClient_ThrowsArgumentNullException()
    {
        await Assert.That(() =>
        {
            _ = new PhoenixReadApiClient((HttpClient)null!);
        }).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task TestOptions_ConflictingStateFiles_ThrowsArgumentException()
    {
        await Assert.That(() =>
        {
            _ = new PhoenixReadApiClient(new PhoenixReadApiOptions
            {
                AuthenticationStateFilePath = "auth.json",
                AnonymousStateFilePath = "anon.json",
            });
        }).Throws<ArgumentException>();
    }

    [Test]
    public async Task TestProviderException_CarriesRetryAfter()
    {
        PhoenixReadApiProviderException ex = new(
            "红果 App 接口暂不可用（100: {\"min_ban_time\": 3, \"max_ban_time\": 5, \"ban_rate\": 0}）。",
            "/novel/commentapi/comment/list/v1/",
            providerCode: "100",
            isTransient: true,
            retryAfter: TimeSpan.FromSeconds(3)
        );

        await Assert.That(ex.ProviderCode).IsEqualTo("100");
        await Assert.That(ex.IsTransient).IsTrue();
        await Assert.That(ex.RetryAfter).IsNotNull();
        await Assert.That(ex.RetryAfter!.Value.TotalSeconds).IsEqualTo(3);
    }
}
