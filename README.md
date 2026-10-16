# PhoenixReadApi

`PhoenixReadApi` 是一个面向 .NET 10 的红果短剧客户端类库，提供：

- 分类剧库、详情和分集；
- 视频播放、搜索联想、推荐和弹幕；
- 综合搜索、搜索联想、季数补齐和推荐分页；
- CENC AES-128 内容密钥提取与弹幕时间窗口；
- 可注入 `HttpClient`，支持代理、Cookie、诊断和离线测试；
- 内部 JSON 处理使用 `System.Text.Json` 源代码生成，支持 NativeAOT 和裁剪发布。

## 快速开始

```csharp
using PhoenixReadApi;

using var client = new PhoenixReadApiClient();
using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
var catalog = await client.GetCatalogAsync(cancellationToken: timeout.Token);
var details = await client.GetDetailsAsync(catalog[0].Id, timeout.Token);
var media = await client.ResolveMediaAsync(details.Drama.Id, details.Chapters[0].VideoId, timeout.Token);

Console.WriteLine($"{details.Drama.Title}: {media.Url}");
```

应用已经拥有 `HttpClient` 时直接注入：

```csharp
var options = new PhoenixReadApiOptions
{
    MaxPagesPerCategory = 10,
    RequestTimeout = TimeSpan.FromSeconds(15)
};
using var client = new PhoenixReadApiClient(httpClient, options);
```

库会校验外部数据、剧集归属和播放地址，并限制响应大小；失败时抛出 `PhoenixReadApiException`，异常中包含请求位置和可用的 HTTP 状态码。

## NativeAOT

示例项目开启了 `<PublishAot>true</PublishAot>`。库返回的公开模型由客户端直接构造，内部 JSON 处理使用源代码生成的 `JsonSerializerContext`。库没有为公开模型提供反射 JSON 序列化路径，应用如需 JSON 输出可以在自己的 NativeAOT `JsonSerializerContext` 中登记这些模型。

```powershell
dotnet publish PhoenixReadApi.Sample -c Release -r win-x64
```

网络接口会随红果服务端变化。生产环境建议注入自己的 `HttpClient`，设置超时和代理，并记录 `PhoenixReadApiException.Endpoint` 与 `StatusCode`。

## 错误处理

所有失败都会抛出 `PhoenixReadApiException` 或其派生类型。异常提供 `Operation`、`RequestUri`、`StatusCode` 和 `IsTransient` 属性，便于记录日志并决定是否重试。
