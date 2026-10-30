# PhoenixReadApi

[![NuGet Version](https://img.shields.io/nuget/v/PhoenixReadApi.svg?style=flat-square&color=blue)](https://www.nuget.org/packages/PhoenixReadApi)
[![NuGet Downloads](https://img.shields.io/nuget/dt/PhoenixReadApi.svg?style=flat-square&color=blue)](https://www.nuget.org/packages/PhoenixReadApi)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4.svg?style=flat-square)](https://dotnet.microsoft.com/)
[![NativeAOT Ready](https://img.shields.io/badge/NativeAOT-Compatible-2ea44f.svg?style=flat-square)](https://learn.microsoft.com/dotnet/core/deploying/native-aot/)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg?style=flat-square)](LICENSE)

`PhoenixReadApi` 是一个面向 .NET 10 的现代化、轻量级红果短剧（PhoenixRead）客户端类库。它封装了红果短剧服务端的核心能力，提供剧库浏览、短剧检索、多清晰度播放地址解析、CENC 媒体内容密钥提取、弹幕抓取、评论互动、书架与播放进度同步，以及开箱即用的登录会话持久化支持。

### 设计理念

- **极致性能与 NativeAOT 原生兼容**：内部 JSON 协议处理完全依赖 `System.Text.Json` 源代码生成（Source Generation），零动态反射，支持快速冷启动与单文件裁剪发布。
- **清晰稳定的领域模型**：向调用方暴露结构清晰的强类型模型，屏蔽多变的下层协议细节与冗余兼容字段。
- **高弹性与双通道容错**：内置 App 与 Web 双通道混合调度，支持只读操作的幂等重试，保障网络波动下的请求成功率。
- **灵活的生命周期管理**：原生支持外部 `HttpClient` 注入、代理配置以及桌面端/命令行场景下的跨会话凭据无感恢复。

---

## 目录

- [特性一览](#特性一览)
- [安装指南](#安装指南)
- [快速上手](#快速上手)
- [核心场景与代码示例](#核心场景与代码示例)
  - [1. 短剧分类、标签筛选与排行榜](#1-短剧分类标签筛选与排行榜)
  - [2. 短剧检索与智能联想](#2-短剧检索与智能联想)
  - [3. 剧集详情、批量信息与演职员署名](#3-剧集详情批量信息与演职员署名)
  - [4. 视频播放解析与 CENC 内容解密](#4-视频播放解析与-cenc-内容解密)
  - [5. 弹幕抓取与评论互动](#5-弹幕抓取与评论互动)
  - [6. 书架、阅读历史与播放进度](#6-书架阅读历史与播放进度)
  - [7. 账号登录与会话持久化](#7-账号登录与会话持久化)
- [配置选项与依赖注入](#配置选项与依赖注入)
  - [PhoenixReadApiOptions 配置表](#phoenixreadapioptions-配置表)
  - [与 IHttpClientFactory 集成](#与-ihttpclientfactory-集成)
- [NativeAOT 与裁剪发布](#nativeaot-与裁剪发布)
- [异常处理体系](#异常处理体系)
- [免责声明与许可](#免责声明与许可)

---

## 特性一览

- 🎬 **短剧发现与检索**
  - 分类剧库（真人短剧、漫剧、AI 短剧）及题材标签树（热门、情节、主题、角色等）多维筛选；
  - 排行榜榜单（热播榜、好评榜、飙升榜、新剧榜）及新剧日历浏览；
  - 关键词精准/模糊搜索、智能搜索联想、短剧推荐流与缺失季数自动补齐。
- 📺 **媒体播放解析**
  - 高画质视频播放直链解析与备选码率（720p / 1080p 等）列表；
  - 针对 CENC 加密媒体，自动提取 AES-128 内容解密密钥，便于播放内核或外部工具（如 FFmpeg）解密。
- 💬 **弹幕与社区互动**
  - 分集弹幕按毫秒时间窗口抓取、自动合并与去重；
  - 剧评与分集评论浏览（支持最热/最新排序）、多级嵌套回复树分页获取；
  - 支持发表评论、回复、评论点赞与删除等互动操作。
- 📚 **书架与进度同步**
  - 短剧观看与阅读历史记录查询与删除；
  - 分集播放进度毫秒级上报与批量剧集进度查询；
  - 追剧收藏夹管理、新剧预约与追更日历。
- 👤 **创作者与用户主页**
  - 演职员署名解析（演员、导演、编剧、出品方及角色名）；
  - 创作者/用户主页、动态发布记录、关联作品、公开收藏与点赞列表。
- 🔐 **认证与多端会话**
  - 手机号短信验证码登录与网页扫码登录（二维码会话轮询）；
  - 登录会话与匿名设备快照（Device ID / Install ID）原子写入持久化，跨进程无缝续期。

---

## 安装指南

### .NET CLI
```bash
dotnet add package PhoenixReadApi
```

### Package Manager
```powershell
Install-Package PhoenixReadApi
```

### PackageReference
```xml
<PackageReference Include="PhoenixReadApi" Version="1.0.0" />
```

---

## 快速上手

以下示例演示如何创建客户端、检索短剧、获取分集列表并解析视频播放直链：

```csharp
using PhoenixReadApi;

// 1. 初始化客户端（默认自动管理底层 HttpClient）
using var client = new PhoenixReadApiClient();
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

// 2. 搜索短剧
SearchResult search = await client.SearchAsync("请君入我怀", cts.Token);
Drama? drama = search.Items.FirstOrDefault();

if (drama is not null)
{
    Console.WriteLine($"找到短剧: 《{drama.Title}》 (总集数: {drama.EpisodeCount})");

    // 3. 获取剧集详情与分集列表
    DramaDetails details = await client.GetDramaDetailsAsync(drama.Id, cts.Token);
    Episode? firstEpisode = details.Episodes.FirstOrDefault();

    if (firstEpisode is not null)
    {
        Console.WriteLine($"第 {firstEpisode.Number} 集: {firstEpisode.Title}");

        // 4. 解析第一集的视频播放信息
        PlaybackMedia media = await client.ResolveMediaAsync(drama.Id, firstEpisode.VideoId, cts.Token);
        Console.WriteLine($"播放直链: {media.Url}");
        Console.WriteLine($"媒体时长: {media.Duration}, 画质得分: {media.Quality}");

        // 若分集为 CENC 加密媒体，可取得对应的 AES-128 解密密钥
        if (!string.IsNullOrEmpty(media.DecryptionKey))
        {
            Console.WriteLine($"CENC 解密密钥: {media.DecryptionKey}");
        }
    }
}
```

---

## 核心场景与代码示例

### 1. 短剧分类、标签筛选与排行榜

#### 分类剧库与标签筛选
```csharp
// 浏览真人短剧剧库
IReadOnlyList<Drama> shortDramas = await client.GetCatalogAsync(
    PhoenixReadApiCategory.ShortDrama,
    cancellationToken: cts.Token
);

// 获取全站题材分类标签树（例如：热门、情节、主题、角色等标签组）
IReadOnlyList<TagCategoryGroup> tagGroups = await client.GetTagCategoriesAsync(cts.Token);
foreach (TagCategoryGroup group in tagGroups)
{
    Console.WriteLine($"标签组: {group.Name}");
    foreach (TagCategoryItem item in group.Items.Take(5))
    {
        Console.WriteLine($"  - [{item.Id}] {item.Name}");
    }
}

// 根据标签 ID 分页检索短剧（例如筛选“都市日常”）
TagDramaPage page = await client.GetTagDramaPageAsync(
    tagItemId: "cate_261",
    sortType: "new_sort_hot", // 排序方式：最热 "new_sort_hot"，最新 "new_sort_latest"
    offset: 0,
    count: 12,
    cancellationToken: cts.Token
);

Console.WriteLine($"当前页返回 {page.Items.Count} 部短剧，是否还有更多: {page.HasMore}");
```

#### 榜单与上新日历
```csharp
// 获取首页精选榜单（热播榜、好评榜、飙升榜、新剧榜等）
FeedPage feedCategories = await client.GetFeedCategoriesAsync(cts.Token);
foreach (FeedSection section in feedCategories.Sections)
{
    Console.WriteLine($"★ 榜单: {section.Title} (共 {section.Items.Count} 部短剧)");
    foreach (Drama drama in section.Items.Take(3))
    {
        Console.WriteLine($"   TOP: 《{drama.Title}》 - 评分: {drama.Score}");
    }
}

// 获取待播新剧日历与可预约列表
ReservationPage upcoming = await client.GetUpcomingReservationsAsync(cts.Token);
foreach (Reservation drama in upcoming.Items.Take(5))
{
    Console.WriteLine($"新剧: 《{drama.Title}》 (ID: {drama.SeriesId}), 预约状态: {drama.IsActive}");
}
```

---

### 2. 短剧检索与智能联想

```csharp
// 实时搜索联想（适用于搜索框输入下拉提示）
IReadOnlyList<SearchSuggestion> suggestions = await client.GetSuggestionsAsync("战神", maxResults: 5, cts.Token);
foreach (SearchSuggestion suggestion in suggestions)
{
    Console.WriteLine($"建议词: {suggestion.Name} (类型: {suggestion.Type})");
}

// 综合短剧搜索
SearchResult searchResult = await client.SearchAsync("逆袭", cts.Token);
Console.WriteLine($"搜索命中: {searchResult.Total} 部短剧 (是否受限: {searchResult.Limited})");

// 获取指定题材推荐流（支持基于上一页游标与已展示 ID 集合去重）
RecommendationPage recommendations = await client.GetRecommendationsAsync(
    genre: RecommendationGenre.ShortDrama,
    offset: 0,
    cancellationToken: cts.Token
);
```

---

### 3. 剧集详情、批量信息与演职员署名

```csharp
// 获取剧集元数据及全部分集列表
DramaDetails details = await client.GetDramaDetailsAsync("74123456789", cts.Token);

// 演职员署名信息（演员、导演、编剧、出品方及出演角色）
foreach (DramaCredit credit in details.Drama.Credits)
{
    string roles = string.Join(", ", credit.Roles.Select(r => $"{r.Type}: {r.CharacterName}"));
    Console.WriteLine($"演职员: {credit.User.Name} ({roles})");

    // 支持直接根据署名对象或用户 ID 查询演职员主页
    UserProfile profile = await client.GetUserProfileAsync(credit, cts.Token);
    Console.WriteLine($"  关联作品数: {profile.RelatedDramas.Count}");
}

// 批量获取多部剧集基础信息（单次最多 50 部）
IReadOnlyList<Drama> batchDramas = await client.GetBatchDramaDetailsAsync(
    ["74123456789", "74987654321"],
    cts.Token
);
```

---

### 4. 视频播放解析与 CENC 内容解密

```csharp
// 解析指定分集的可播放媒体信息
PlaybackMedia media = await client.ResolveMediaAsync(
    seriesId: details.Drama.Id,
    videoId: details.Episodes[0].VideoId,
    cancellationToken: cts.Token
);

Console.WriteLine($"主播放地址: {media.Url}");
Console.WriteLine($"必要 Referer: {media.Referer}");
Console.WriteLine($"视频时长: {media.Duration}");

// 获取全部可用清晰度版本
foreach (MediaVariant variant in media.Variants)
{
    Console.WriteLine($"  清晰度得分: {variant.Quality}, 地址: {variant.Url}");
}

// 若视频采用 CENC 加密，DecryptionKey 包含 16 字节十六进制 AES-128 解密密钥
// 可直接用于 FFmpeg 等播放/转码工具：
if (!string.IsNullOrEmpty(media.DecryptionKey))
{
    Console.WriteLine($"CENC 解密密钥: {media.DecryptionKey}");
    // 对应 FFmpeg 命令行示例：
    // ffmpeg -decryption_key <media.DecryptionKey> -headers "Referer: <media.Referer>" -i "<media.Url>" output.mp4
}
```

---

### 5. 弹幕抓取与评论互动

#### 抓取分集弹幕
```csharp
string seriesId = "74123456789";
string videoId = "74987654321";

// 抓取视频指定时间窗口内的弹幕（毫秒单位；例如获取前 10 分钟弹幕）
DanmakuPage danmaku = await client.GetDanmakuAsync(
    seriesId,
    videoId,
    startMs: 0,
    durationMs: 600_000,
    cancellationToken: cts.Token
);

Console.WriteLine($"获取到弹幕: {danmaku.Items.Count} 条 (服务端报告总量: {danmaku.Total})");
foreach (DanmakuItem item in danmaku.Items.Take(5))
{
    Console.WriteLine($"[{item.OffsetMs}ms] {item.Text}");
}
```

#### 剧评与分集评论
```csharp
// 分页获取剧评（支持最热/最新排序）
CommentPage reviews = await client.GetDramaReviewsAsync(
    seriesId,
    count: 10,
    sort: CommentSortOrder.Hot,
    cancellationToken: cts.Token
);

// 获取分集评论
CommentPage episodeComments = await client.GetEpisodeCommentsAsync(
    seriesId,
    videoId,
    count: 10,
    sort: CommentSortOrder.Hot,
    cancellationToken: cts.Token
);

// 获取某条评论的二级回复
if (reviews.Items.FirstOrDefault() is { } topComment && topComment.ReplyCount > 0)
{
    CommentReplyPage replies = await client.GetDramaReviewRepliesAsync(
        topComment.Id,
        seriesId,
        count: 10,
        cancellationToken: cts.Token
    );
    Console.WriteLine($"评论《{topComment.Text}》共有 {replies.Items.Count} 条回复");
}

// 互动操作（需在登录状态下调用）
await client.SendEpisodeCommentAsync(seriesId, videoId, "剧情太紧凑了！", cts.Token);
await client.SetCommentLikeAsync(topComment.Id, seriesId, videoId, active: true, cts.Token);
```

---

### 6. 书架、阅读历史与播放进度

```csharp
// 查询个人观看/阅读历史
ReadHistoryPage history = await client.GetHistoryAsync(cancellationToken: cts.Token);

// 上报当前分集的播放进度
await client.ReportProgressAsync(new ProgressUpdate
{
    SeriesId = seriesId,
    VideoId = videoId,
    EpisodeNumber = 1,
    ProgressSeconds = 45,
    LastReadTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
}, cts.Token);

// 批量查询指定剧集或分集的上次播放进度
ReadProgressPage progressPage = await client.GetReadProgressForAsync([seriesId], cts.Token);

// 收藏短剧
await client.SetFavoriteAsync(seriesId, videoId, active: true, cts.Token);

// 查询收藏夹列表
DramaPage favorites = await client.GetFavoritesAsync(cancellationToken: cts.Token);
```

---

### 7. 账号登录与会话持久化

`PhoenixReadApi` 提供两种登录方式：网页扫码登录与手机短信验证码登录。

#### 方式一：网页扫码登录（推荐用于桌面应用/命令行工具）

```csharp
// 1. 发起扫码会话，获取二维码图片与轮询凭证
QrCodeLoginSession qrSession = await client.CreateQrCodeLoginAsync(cts.Token);

// qrSession.QrCodeBase64 为二维码图片的 Base64 编码，qrSession.QrCodeImageBytes 为图片原始字节
Console.WriteLine($"请使用红果短剧 App 扫码（有效时间: {qrSession.ExpiresInSeconds} 秒）");

// 2. 轮询扫码状态
while (!cts.Token.IsCancellationRequested)
{
    await Task.Delay(2000, cts.Token);
    QrCodeCheckResult status = await client.CheckQrCodeStatusAsync(qrSession.Token, cts.Token);

    if (status.Status == QrCodeLoginStatus.Confirmed)
    {
        Console.WriteLine($"登录成功！欢迎: {status.User?.Name}");
        break;
    }

    if (status.Status == QrCodeLoginStatus.Expired || status.Status == QrCodeLoginStatus.Failed)
    {
        Console.WriteLine($"扫码已失效或取消: {status.Status}");
        break;
    }
}
```

#### 方式二：手机短信验证码登录

```csharp
// 1. 发送短信验证码
VerificationCodeResult verifyResult = await client.SendVerificationCodeAsync("13800000000", cts.Token);
Console.WriteLine($"验证码已发送至 {verifyResult.MaskedPhone}，重发等待: {verifyResult.RetryIntervalSeconds}s");

// 2. 使用收到的验证码登录
AuthenticatedUser user = await client.LoginAsync("13800000000", "123456", cts.Token);
Console.WriteLine($"登录成功: {user.Name} (ID: {user.Id})");
```

#### 会话持久化与匿名设备快照

在客户端配置中指定 `AuthenticationStateFilePath`，成功登录后会话将以原子方式持久化到本地文件。下次应用重启时，客户端会自动读取并恢复 Cookie 与 Passport 凭证。

若无需登录但希望在多次运行间维持相同的设备指纹与未登录观看进度，可配置 `AnonymousStateFilePath`（二者互斥）：

```csharp
var options = new PhoenixReadApiOptions
{
    // 配置文件持久化路径（文件内包含认证凭据，请妥善保护文件系统访问权限）
    AuthenticationStateFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MyApp", "session.json"
    )
};

using var client = new PhoenixReadApiClient(options);

// 启动时校验已恢复会话在服务端的有效性
bool isSessionValid = await client.ValidateSessionAsync(cts.Token);
if (isSessionValid)
{
    CurrentUserInfo user = await client.GetCurrentUserInfoAsync(cts.Token);
    Console.WriteLine($"已自动恢复登录身份: {user.Name} (VIP: {user.IsVip})");
}

// 退出登录：清空内存凭据并安全删除本地会话文件
client.Logout();
```

---

## 配置选项与依赖注入

### PhoenixReadApiOptions 配置表

| 配置项 | 类型 | 默认值 | 说明 |
| :--- | :--- | :--- | :--- |
| `RequestTimeout` | `TimeSpan` | `20s` | 单次 HTTP 请求的整体截止时间（包含响应体读取） |
| `RetryCount` | `int` | `2` | 只读请求遇到临时错误时的最大尝试次数（非幂等写操作不自动重试） |
| `MaxResponseBytes` | `int` | `8 MB` | 常规接口允许接收的最大响应体大小，避免异常数据占用过多内存 |
| `EnableWebFallback` | `bool` | `true` | 当主要 App 接口请求异常时，是否自动尝试 Web 网页接口进行兜底 |
| `CompleteSearchSeasons` | `bool` | `true` | 短剧搜索时是否自动分析并补齐缺失的季数数据 |
| `MaxPagesPerCategory` | `int` | `50` | 单个分类剧库请求时最多检索的页数上限 |
| `AuthenticationStateFilePath` | `string?` | `null` | 登录会话本地持久化快照路径（与匿名路径互斥） |
| `AnonymousStateFilePath` | `string?` | `null` | 匿名设备会话本地持久化快照路径（与登录路径互斥） |
| `DeviceId` | `string?` | 自动生成 | 自定义或固定设备标识（纯数字字符串） |
| `InstallId` | `string?` | 自动生成 | 自定义或固定安装标识（纯数字字符串） |
| `UserAgent` | `string` | 预设 Android UA | 发送请求时使用的 User-Agent 请求头 |

### 与 IHttpClientFactory 集成

在 ASP.NET Core 或依赖注入容器中，推荐将 `PhoenixReadApiClient` 注册为 Typed Client，复用全局连接池并配置网络代理：

```csharp
// Program.cs
builder.Services.AddHttpClient<PhoenixReadApiClient>((sp, httpClient) =>
{
    // 配置全局超时或自定义标头
})
.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
{
    PooledConnectionLifetime = TimeSpan.FromMinutes(15),
    // 如有需要可在此处配置代理服务器：
    // Proxy = new WebProxy("http://127.0.0.1:8888")
});

builder.Services.AddSingleton(new PhoenixReadApiOptions
{
    RequestTimeout = TimeSpan.FromSeconds(15),
    RetryCount = 3
});
```

---

## NativeAOT 与裁剪发布

`PhoenixReadApi` 在设计之初即全面遵循 NativeAOT 与无修剪警告（Trim-compatible）规范：

- **内部零反射**：库内部对服务端所有 JSON 报文的解析和序列化全部由 Source Generator（`JsonSerializerContext`）在编译阶段预生成；
- **公开模型序列化指南**：若消费端应用自身开启了 NativeAOT，并且需要将本库的公开模型（如 `Drama`、`DramaDetails`、`PlaybackMedia` 等）序列化为 JSON 输出，只需在应用自有的 `JsonSerializerContext` 中登记相关类型：

```csharp
using System.Text.Json.Serialization;
using PhoenixReadApi;

[JsonSerializable(typeof(Drama))]
[JsonSerializable(typeof(DramaDetails))]
[JsonSerializable(typeof(PlaybackMedia))]
[JsonSerializable(typeof(IReadOnlyList<Drama>))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{
}
```

---

## 异常处理体系

本库中所有由网络通信、协议反序列化、业务拒绝引起的异常均继承自 `PhoenixReadApiException`：

```text
PhoenixReadApiException
├── PhoenixReadApiTransportException       # 底层网络传输与连接建立异常
├── PhoenixReadApiHttpException            # HTTP 状态码非成功异常 (4xx, 5xx)
├── PhoenixReadApiResponseException         # 响应结构不符合业务协议要求
├── PhoenixReadApiDeserializationException   # JSON 报文反序列化失败
├── PhoenixReadApiProviderException        # 服务端返回明确业务错误码
│   └── PhoenixReadApiAuthenticationException # 登录态失效或设备认证被拒绝
├── PhoenixReadApiMediaException           # 媒体流直链或解密密钥解析失败
└── PhoenixReadApiFallbackException        # App 与 Web 双通道兜底均宣告失败
```

### 异常属性与处理示例

```csharp
try
{
    DramaDetails details = await client.GetDramaDetailsAsync("invalid_id", cts.Token);
}
catch (PhoenixReadApiAuthenticationException ex)
{
    // 身份认证失效或触发风控，需引导重新登录
    Console.WriteLine($"认证失败: {ex.Message}, 业务错误码: {ex.ProviderCode}");
}
catch (PhoenixReadApiException ex)
{
    Console.WriteLine($"操作 [{ex.Operation}] 执行失败:");
    Console.WriteLine($"  请求地址: {ex.RequestUri}");
    Console.WriteLine($"  HTTP 状态码: {ex.StatusCode}");
    Console.WriteLine($"  业务错误码: {ex.ProviderCode}");
    Console.WriteLine($"  是否可安全重试: {ex.IsTransient}");
}
```

---

## 免责声明与许可

- **免责声明**：本项目为非官方的开源短剧 API 客户端，仅供个人学习、技术研究与自动化测试使用。接口协议可能随官方服务端升级而调整。使用本库时请自觉遵守红果短剧及字节跳动相关服务协议与法律法规，请勿用于非法用途或商业竞争。
- **开源协议**：本项目基于 [MIT License](LICENSE) 许可开源。
