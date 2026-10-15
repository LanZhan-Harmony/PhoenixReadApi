# PhoenixReadApi

`PhoenixReadApi` 是一个面向 .NET 10 的红果短剧客户端类库，提供：

- App 分类剧库、详情和分集；
- App 播放模型、网页播放器和备用播放接口的自动兜底；
- 综合搜索、搜索联想、季数补齐和推荐分页；
- CENC AES-128 内容密钥提取与弹幕时间窗口；
- 可注入 `HttpClient`，支持代理、Cookie、诊断和离线测试；
- 内部 App 请求与搜索联想响应使用 `System.Text.Json` source generation，支持 NativeAOT 和裁剪发布。

## 快速开始

```csharp
using PhoenixReadApi;
using PhoenixApi = PhoenixReadApi.PhoenixReadApi;

using var client = new PhoenixApi();
var catalog = await client.GetCatalogAsync(cancellationToken);
var details = await client.GetDetailsAsync(catalog[0].SourceId, cancellationToken);
var media = await client.ResolveMediaAsync(details.Drama.SourceId, details.Chapters[0].VideoUrl, cancellationToken);

Console.WriteLine($"{details.Drama.DisplayTitle}: {media.Url}");
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

库不会执行网页 JavaScript，只读取页面内的 `_ROUTER_DATA` JSON。所有外部响应都会限制大小、校验 ID/分页/剧集归属和媒体 URL；失败时抛出 `PhoenixReadApiException`，其中包含逻辑端点和可用的 HTTP 状态码。

## NativeAOT

Samples 项目开启了 `<PublishAot>true</PublishAot>`。库返回的公开模型由客户端直接构造，不参与后端 JSON 反序列化；只有内部 App 请求模型和搜索联想响应登记在 source-generated `JsonSerializerContext` 中。库没有为公开模型提供反射 JSON 序列化路径，应用如需 JSON 输出可以在自己的 NativeAOT `JsonSerializerContext` 中登记这些模型。

```powershell
dotnet publish PhoenixReadApi.Sample -c Release -r win-x64
```

网络接口会随上游服务变化。生产环境建议注入自己的 `HttpClient`、设置超时与代理，并记录 `PhoenixReadApiException.Endpoint` 和 `StatusCode`。

## 内部架构

公共 API 仍然由 `PhoenixReadApiClient` 和 `PhoenixReadApiOptions` 提供，内部按职责分为：

- `Features/Catalog`、`Details`、`Search`、`Playback` 和 `Danmaku` 服务；
- `Infrastructure/Http` 中统一处理超时、重试、HTTP 状态和响应大小；
- `Infrastructure/Crypto` 中分别处理设备 ID、请求签名、内容密钥和备用播放解码；
- `Contracts/App`、`Contracts/Web` 中的内部 DTO，以及 `Serialization` 中的 source-generated JSON 上下文和局部兼容转换器；
- `Infrastructure/Web` 中的 `_ROUTER_DATA` 提取和动态 loader 定位。

App 和网页数据在传输边界反序列化为内部 DTO，再由 mapper 转成稳定的公开模型。网页 HTML 只在 `RouterDataExtractor` 边界使用 JSON 文档。

异常保留 `PhoenixReadApiException` 作为兼容基类，并提供 `PhoenixReadApiTransportException`、`PhoenixReadApiHttpException`、`PhoenixReadApiProviderException`、`PhoenixReadApiDeserializationException`、`PhoenixReadApiResponseException`、`PhoenixReadApiMediaException` 和 `PhoenixReadApiFallbackException`，同时提供 `Operation`、`RequestUri`、`ProviderCode` 和 `IsTransient` 元数据。
