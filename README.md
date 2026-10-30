# PhoenixReadApi

`PhoenixReadApi` 是一个面向 .NET 10 的红果短剧 API Nuget 库，提供：

- 分类剧库、详情和分集；
- 视频播放、搜索联想、推荐和弹幕；
- 综合搜索、搜索联想、季数补齐和推荐分页；
- 剧集署名账号、账号主页、剧评和分集评论；
- 阅读历史、播放进度、收藏、点赞、预约和剧集分季；
- 评论与回复（支持剧评回复与分集评论回复）、评论操作、分类筛选、首页分组和分享目标解析；
- 短信登录状态管理（登录服务需要设备认证时会明确返回认证异常）；
- CENC AES-128 内容密钥提取与弹幕时间窗口；
- 可注入 `HttpClient`，支持代理、Cookie、诊断和离线测试；
- 内部 JSON 处理使用 `System.Text.Json` 源代码生成，支持 NativeAOT 和裁剪发布。

## 警告

目前红果服务端的接口不稳定，部分接口可能随时下线或更改。库会尽量兼容服务端变化，但不保证所有接口都可用。请在使用前确认接口是否可用。

## 快速开始

```csharp
using PhoenixReadApi;

using var client = new PhoenixReadApiClient();
using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
var catalog = await client.GetCatalogAsync(cancellationToken: timeout.Token);
var details = await client.GetDetailsAsync(catalog[0].Id, timeout.Token);
var media = await client.ResolveMediaAsync(details.Drama.Id, details.Episodes[0], timeout.Token);
var reviews = await client.GetDramaReviewsAsync(details.Drama.Id, cancellationToken: timeout.Token);
var episodeComments = await client.GetEpisodeCommentsAsync(
    details.Drama.Id,
    details.Episodes[0].VideoId,
    cancellationToken: timeout.Token
);

Console.WriteLine($"{details.Drama.Title}: {media.Url}");
Console.WriteLine($"Reviews: {reviews.Items.Count}/{reviews.Total}");
Console.WriteLine($"Episode comments: {episodeComments.Items.Count}/{episodeComments.Total}");

var history = await client.GetHistoryAsync(cancellationToken: timeout.Token);
var favorites = await client.GetFavoritesAsync(cancellationToken: timeout.Token);
var progress = await client.GetReadProgressAsync(timeout.Token);
```

推荐题材和搜索建议类型使用枚举：

```csharp
var recommendations = await client.GetRecommendationsAsync(
    RecommendationGenre.ShortDrama,
    cancellationToken: timeout.Token
);
var suggestions = await client.GetSuggestionsAsync("请君入我怀", cancellationToken: timeout.Token);
SearchSuggestionType suggestionType = suggestions[0].Type;
DramaReleaseStatus releaseStatus = details.Drama.ReleaseStatus;
```

详情中的 `Drama.Credits` 包含与剧集有关的账号及其署名关系。账号可能是演员、导演、
编剧或出品方，出演角色保存在 `DramaCreditRole.CharacterName`。所有账号都使用统一的
`User` 模型，可以直接读取其主页：

```csharp
DramaCredit credit = details.Drama.Credits[0];
var profile = await client.GetUserProfileAsync(credit, timeout.Token);
// 也可以直接传入账号 ID
var sameProfile = await client.GetUserProfileAsync(credit.User.Id, timeout.Token);
```

剧评和分集评论支持游标分页。将返回页的 `Cursor` 传给下一次调用即可：

```csharp
var nextReviews = await client.GetDramaReviewsAsync(
    details.Drama.Id,
    cursor: reviews.Cursor,
    cancellationToken: timeout.Token
);
```

账号主页的收藏、点赞、动态和关联剧集支持分页。动态和关联剧集使用游标，收藏和点赞使用偏移量：

```csharp
string userId = credit.User.Id;
var activityPage = await client.GetUserActivitiesAsync(userId, cancellationToken: timeout.Token);
if (activityPage.HasMore)
{
    var nextActivities = await client.GetUserActivitiesAsync(
        userId,
        cursor: activityPage.Cursor,
        cancellationToken: timeout.Token
    );
}

var collectionPage = await client.GetUserCollectionsAsync(
    userId,
    offset: 0,
    cancellationToken: timeout.Token
);
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

## 登录状态持久化

短信登录成功后，如果希望程序重启后继续使用同一个登录状态，可以指定本地会话备份文件：

```csharp
var options = new PhoenixReadApiOptions
{
    AuthenticationStateFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "PhoenixReadApi",
        "session.json"
    )
};

using var client = new PhoenixReadApiClient(options);
var verification = await client.SendVerificationCodeAsync("手机号");
var account = await client.LoginAsync("手机号", "短信验证码");
```

下次使用相同的 `AuthenticationStateFilePath` 创建客户端时，库会自动恢复本地 Cookie 和 Passport Token。会话文件包含敏感认证信息，应限制文件访问权限。调用 `Logout()` 会同时清除内存会话和该文件。

如果不登录也需要持续使用同一个匿名设备会话，可以单独指定 `AnonymousStateFilePath`，不要与登录会话文件混用：

```csharp
var options = new PhoenixReadApiOptions
{
    AnonymousStateFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "PhoenixReadApi",
        "anonymous-session.json"
    )
};
```

匿名会话首次创建时会生成独立的设备/安装 ID，并把它们与服务端返回的匿名 Cookie 一起保存；再次使用同一路径会恢复同一设备会话。它可以用于未登录的历史、播放进度、收藏、预约和点赞请求；服务端仍可能对写操作进行风控。登录会话路径和匿名会话路径只能设置其中一个。

库会校验外部数据、剧集归属和播放地址，并限制响应大小；失败时抛出 `PhoenixReadApiException`，异常中包含请求位置和可用的 HTTP 状态码。

## NativeAOT

示例项目开启了 `<PublishAot>true</PublishAot>`。库返回的公开模型由客户端直接构造，内部 JSON 处理使用源代码生成的 `JsonSerializerContext`。库没有为公开模型提供反射 JSON 序列化路径，应用如需 JSON 输出可以在自己的 NativeAOT `JsonSerializerContext` 中登记这些模型。

```powershell
dotnet publish PhoenixReadApi.Sample -c Release -r win-x64
```

网络接口会随红果服务端变化。生产环境建议注入自己的 `HttpClient`，设置超时和代理，并记录 `PhoenixReadApiException.Endpoint` 与 `StatusCode`。

## 错误处理

所有失败都会抛出 `PhoenixReadApiException` 或其派生类型。异常提供 `Operation`、`RequestUri`、`StatusCode` 和 `IsTransient` 属性，便于记录日志并决定是否重试。
