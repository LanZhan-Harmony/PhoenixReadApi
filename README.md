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
  - [8. 演职人员与用户主页、作品多 Tab 检索](#8-演职人员与用户主页作品多-tab-检索)
  - [9. 剧集追更与通知中心](#9-剧集追更与通知中心)
  - [10. 表情包与评论图片上传](#10-表情包与评论图片上传)
  - [11. 负反馈与不感兴趣推荐过滤](#11-负反馈与不感兴趣推荐过滤)
  - [12. 跨页去重流式遍历](#12-跨页去重流式遍历)
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
  - 支持发表评论、回复、评论点赞与删除等互动操作；
  - 评论表情包查询（热门与收藏表情包）、表情包收藏与取消、评论图片文件 Multipart 上传。
- 📚 **书架、追更与通知中心**
  - 短剧观看与阅读历史记录查询与删除；
  - 分集播放进度毫秒级上报与批量剧集进度查询；
  - 追剧收藏夹管理、新剧预约与追更日历；
  - 剧集更新追更提醒订阅与取消、追更短剧列表分页、短剧更新通知检查与通知中心未读消息管理。
- 👤 **创作者与演职人员主页**
  - 演职员署名解析（演员、导演、编剧、出品方及角色名）；
  - 演职人员/用户主页完整档案（角色标签、蓝V认证、红果号、关注数、粉丝数、获赞数、主页背景图与成就勋章）；
  - 作品多 Tab 分页查询（全部、剧作、预约、合集、剧评）及按最新/最热排序；
  - 动态发布记录、公开收藏与点赞列表。
- 🛡️ **负反馈与推荐过滤**
  - 长按负反馈选项面板配置拉取（“我不想看”、“内容问题”等）；
  - 提交不感兴趣反馈（按短剧、演员、分类等原因屏蔽过滤）。
- 🔐 **认证与多端会话**
  - 支持的国家/地区国际区号列表查询（中国大陆、香港、澳门、台湾及海外主要国家）；
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
Drama? drama = search.Dramas.FirstOrDefault();

if (drama is not null)
{
    Console.WriteLine($"找到短剧: 《{drama.Title}》 (总集数: {drama.EpisodeCount})");

    // 3. 获取剧集详情与分集列表
    DramaDetails details = await client.GetDramaDetailsAsync(drama.Id, cts.Token);
    Episode? firstEpisode = details.Episodes.FirstOrDefault();

    if (firstEpisode is not null)
    {
        Console.WriteLine($"第 {firstEpisode.Number} 集: {firstEpisode.Title}");

        // 4. 解析第一集的可播放媒体列表（按清晰度从高到低排序，首项为推荐最佳画质）
        IReadOnlyList<PlaybackMedia> medias = await client.ResolveMediaAsync(drama.Id, firstEpisode.VideoId, cts.Token);
        PlaybackMedia media = medias[0];
        Console.WriteLine($"播放直链: {media.Url}");
        Console.WriteLine($"媒体时长: {media.Duration}, 清晰度: {media.Definition} ({media.Codec})");

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
// 获取待播新剧日历与可预约列表
ReservationPage upcoming = await client.GetUpcomingReservationsAsync(cts.Token);
foreach (Reservation drama in upcoming.Items.Take(5))
{
    Console.WriteLine($"新剧: 《{drama.Title}》 (ID: {drama.SeriesId}), 预约状态: {drama.IsActive}");
}

// 演员排行榜（名次、人气值、代表作品、排名升降）
ActorLeaderboardPage actorBoard = await client.GetActorLeaderboardAsync(offset: 0, cancellationToken: cts.Token);
foreach (ActorLeaderboardItem actor in actorBoard.Items.Take(5))
{
    Console.WriteLine($"TOP {actor.Position}: {actor.Name} ({actor.Popularity}) - 代表作: {actor.RepresentativeWork}");
}

// 首页发现频道流式瀑布流（DiscoverPage，支持推荐、真人剧、漫剧、找剧、小说、听书各频道）
DiscoverPage discover = await client.GetDiscoverPageAsync(
    channel: DiscoverChannel.Recommend,
    cursor: null,
    cancellationToken: cts.Token
);
Console.WriteLine($"推荐频道短剧数: {discover.Dramas.Count}, 下一页游标: {discover.Cursor}");

// 短剧动态多维筛选面板与组合矩阵筛选
IReadOnlyList<ShortDramaFilterRow> filterRows = await client.GetShortDramaFilterPanelAsync(ShortDramaGenre.ShortPlay, cts.Token);
ShortDramaFilterPage filterPage = await client.FilterShortDramasAsync(
    new ShortDramaFilterQuery
    {
        Genre = ShortDramaGenre.ShortPlay,
        Selections = new Dictionary<string, IReadOnlyList<string>> { ["category"] = ["战神"] },
        Offset = 0,
    },
    cts.Token
);

// 按日期指定查询上新日历（查看指定日期的已上线与即将上线作品）
DailyReleasePage dailyPage = await client.GetDailyReleasesAsync(targetDate: new DateOnly(2026, 11, 4), offset: 0, cancellationToken: cts.Token);
foreach (DailyReleaseItem item in dailyPage.Items)
{
    Console.WriteLine($"日历作品: 《{item.Drama.Title}》 - {item.Note} (上线: {item.IsOnline})");
}

// 短剧排行榜（支持热播榜、推荐榜、新剧榜、热搜榜，支持按总榜/女频/男频频道细分）
ShortDramaLeaderboardPage rankBoard = await client.GetShortDramaLeaderboardAsync(
    type: ShortDramaLeaderboardType.HotPlay,
    channel: ShortDramaLeaderboardChannel.Female,
    offset: 0,
    cancellationToken: cts.Token
);
Console.WriteLine($"榜单: {rankBoard.BoardTitle} (返回 {rankBoard.Items.Count} 部短剧)");
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

// 1. 综合短剧搜索（返回短剧、原著小说与明星演员用户）
SearchResult searchResult = await client.SearchAsync("逆袭", cts.Token);
Console.WriteLine($"短剧命中: {searchResult.Dramas.Count} 部 (总计: {searchResult.Total}, 是否受限: {searchResult.Limited})");

// 2. 高级多维度分类搜索（指定 Tab 如原著小说、用户、影视剧及分页游标）
SearchResult novelSearch = await client.SearchAsync(
    new SearchQuery
    {
        Keyword = "逆袭",
        Tab = SearchTab.CommonBook, // 搜索番茄/红果原著小说
        Offset = 0,
        Count = 10,
    },
    cts.Token
);
foreach (Novel novel in novelSearch.Novels)
{
    Console.WriteLine($"原著小说: 《{novel.Title}》 作者: {novel.Author} (连载: {novel.StatusText})");
}

// 搜索明星演职人员
SearchResult userSearch = await client.SearchAsync(
    new SearchQuery
    {
        Keyword = "张",
        Tab = SearchTab.User,
        Count = 5,
    },
    cts.Token
);
foreach (User user in userSearch.Users)
{
    Console.WriteLine($"明星/演员: {user.Name} (ID: {user.Id}) - 简介: {user.Introduction}");
}

// 获取搜索框轮播提示词（滚动热搜词）
IReadOnlyList<string> cues = await client.GetSearchCuesAsync(cts.Token);
Console.WriteLine("轮播热词: " + string.Join(" / ", cues.Take(5)));

// 获取搜索看板热搜榜单与“猜你想搜”
HotSearch hotSearch = await client.GetHotSearchAsync(cts.Token);
foreach (HotSearchBoard board in hotSearch.Boards)
{
    Console.WriteLine($"热搜看板: {board.Title}");
    foreach (HotSearchItem item in board.Items.Take(3))
    {
        Console.WriteLine($"  - {item.Title} [{item.CardTips}]");
    }
}

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
    Console.WriteLine($"  主页作品数: {profile.Works.Count}");
}

// 多季合集与智能聚合（自动获取多季合集中的所有季及分集）
DramaAlbum seasonsAlbum = await client.GetDramaSeasonsAsync("74123456789", cts.Token);
Console.WriteLine($"合集: 《{seasonsAlbum.Title}》 (总季数: {seasonsAlbum.Seasons.Count})");
foreach (DramaSeason season in seasonsAlbum.Seasons)
{
    Console.WriteLine($"  - 第 {season.Index} 季: 《{season.Title}》 ({season.EpisodeCount}集)");
}

// 单剧专属关联推荐（播放器/选集面板“更多好剧”推荐）
DramaRelatedRecommendations related = await client.GetSeriesRecommendationsAsync("74123456789", cts.Token);
foreach (Drama drama in related.Items)
{
    Console.WriteLine($"  - 相关好剧: 《{drama.Title}》 评分: {drama.Score}");
}

// 批量获取多部剧集基础信息（单次最多 50 部）
IReadOnlyList<Drama> batchDramas = await client.GetBatchDramaDetailsAsync(
    ["74123456789", "74987654321"],
    cts.Token
);

// 播放器级批量获取完整详情与全部分集列表（单次最多 50 部）
IReadOnlyList<DramaDetails> batchWithEpisodes = await client.GetBatchDramaDetailsWithEpisodesAsync(
    ["74123456789", "74987654321"],
    cts.Token
);
foreach (DramaDetails item in batchWithEpisodes)
{
    Console.WriteLine($"短剧: 《{item.Drama.Title}》, 分集数: {item.Episodes.Count}");
}
```

---

### 4. 视频播放解析与 CENC 内容解密

```csharp
// 解析指定分集的可播放媒体列表（按画质综合得分从高到低排序）
IReadOnlyList<PlaybackMedia> medias = await client.ResolveMediaAsync(
    seriesId: details.Drama.Id,
    videoId: details.Episodes[0].VideoId,
    cancellationToken: cts.Token
);

// 首个条目即为推荐的最佳画质
PlaybackMedia media = medias[0];
Console.WriteLine($"主播放地址: {media.Url}");
Console.WriteLine($"必要 Referer: {media.Referer}");
Console.WriteLine($"清晰度: {media.Definition} ({media.QualityName}), 编码: {media.Codec}");
Console.WriteLine($"视频时长: {media.Duration}");

// 遍历全部可用清晰度版本（每个条目均自包含完整的播放直链与解密密钥）
foreach (PlaybackMedia variant in medias)
{
    Console.WriteLine($"  [{variant.Definition}] 编码: {variant.Codec}, 码率: {variant.Bitrate}b/s, 地址: {variant.Url}");
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
    Console.WriteLine($"评论《{topComment.Text}》共有 {replies.Items.Count} 条回复，附图数：{topComment.Images.Count}");
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

// 上报当前分集的播放进度（也会同步写入观看历史）
await client.ReportProgressAsync(new ProgressUpdate
{
    SeriesId = seriesId,
    VideoId = videoId,
    EpisodeNumber = 1,
    CurrentPositionMs = 45_000,
    DurationMs = 120_000
}, cts.Token);

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
// 0. 可选：获取支持的国家或地区国际区号列表
IReadOnlyList<LoginRegion> regions = client.GetLoginRegions();

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

// 若遇到服务端匿名频控限制，可主动重置匿名设备并重新执行安全加密注册
await client.ResetDeviceAsync(cts.Token);
```

---

### 8. 演职人员与用户主页、作品多 Tab 检索

红果短剧为演职人员和创作者提供了专业的主页展示，包括角色标签、蓝V认证、红果号、粉丝/关注/获赞统计、荣誉勋章以及专属作品聚合：

```csharp
// 查询指定演员/用户主页完整信息（例如知名短剧演员）
UserProfile profile = await client.GetUserProfileAsync("3974078057440443", cts.Token);

Console.WriteLine($"昵称: {profile.User.Name}");
Console.WriteLine($"主创角色: {string.Join(" | ", profile.CreatorRoles)}"); // 如 "男演员 | 导演"
Console.WriteLine($"红果号: {profile.HongguoId}");
Console.WriteLine($"蓝V认证: {profile.IsVerified}");
Console.WriteLine($"粉丝数: {profile.FansCount}, 获赞数: {profile.LikedCount}");
Console.WriteLine($"主页背景图: {profile.BackgroundCoverUrl}");

// 演职人员专属作品列表（首屏已自动拉取）
foreach (Drama work in profile.Works)
{
    Console.WriteLine($"  - 作品: 《{work.Title}》 集数: {work.EpisodeCount}, 热度: {work.Heat}");
}

// 独立作品分页查询：支持按最新/最热排序，以及子分类（全部、剧作、预约、合集、剧评）
UserWorkPage worksPage = await client.GetUserWorksAsync(
    "3974078057440443",
    new UserWorksQuery
    {
        SubTab = UserWorkSubTab.All,
        Sort = UserWorkSort.Hot,
        Count = 20
    },
    cts.Token
);
Console.WriteLine($"作品总数: {worksPage.Total}, 当前页条数: {worksPage.Items.Count}");

// 获取粉丝列表（支持隐私保护感知）
UserRelationPage followers = await client.GetUserFollowersAsync("3974078057440443", count: 20, cancellationToken: cts.Token);
Console.WriteLine($"隐私保护: {followers.IsPrivacyProtected}, 粉丝条数: {followers.Items.Count}");
foreach (UserRelationItem item in followers.Items)
{
    Console.WriteLine($"  - 粉丝: {item.User.Name} (蓝V: {item.IsVerified}, 主创: {item.IsCreator})");
}

// 获取关注人员列表
UserRelationPage followings = await client.GetUserFollowingsAsync("3974078057440443", count: 20, cancellationToken: cts.Token);
Console.WriteLine($"隐私保护: {followings.IsPrivacyProtected}, 关注人数: {followings.Items.Count}");
foreach (UserRelationItem item in followings.Items)
{
    Console.WriteLine($"  - 关注: {item.User.Name} (关系类型: {(item.IsFollowing ? "已关注" : "未关注")})");
}
```

---

### 9. 剧集追更与通知中心

```csharp
// 开启某部短剧的追更提醒
await client.SetFollowUpdateAsync(dramaId: "7699833115866303550", follow: true, cts.Token);

// 检查是否已开启追更
bool isFollowing = await client.IsFollowingUpdateAsync("7699833115866303550", cts.Token);

// 分页拉取当前追更短剧列表（包含已更新总集数、观看进度与更新频例文案）
FollowUpdatePage followPage = await client.GetFollowUpdatesAsync(cursor: "", count: 20, cts.Token);
foreach (FollowUpdateItem item in followPage.Items)
{
    Console.WriteLine($"追更剧: 《{item.Title}》, 总集数: {item.TotalEpisodeCount}, 状态: {item.UpdateFrequency}");
}

// 检查追更短剧的新更新通知
IReadOnlyList<NoticeItem> newNotices = await client.CheckNoticesAsync(cts.Token);

// 查询通知中心全量通知与未读统计
NoticePage notices = await client.GetNoticesAsync(cts.Token);
Console.WriteLine($"未读通知数: {notices.UnreadCount}");

// 标记已读与清空通知
await client.MarkNoticesReadAsync(notices.Items.Select(n => n.Id).ToList(), cts.Token);
await client.ClearNoticesAsync(cts.Token);
```

---

### 10. 表情包与评论图片上传

```csharp
// 获取评论表情包列表（isCollected: false 为热门表情包，true 为用户已收藏表情包）
EmoticonPage emoticons = await client.GetEmoticonsAsync(isCollected: false, offset: 0, count: 20, cts.Token);
foreach (CommentImage emo in emoticons.Items)
{
    Console.WriteLine($"表情 ID: {emo.Id}, 图片: {emo.Url} ({emo.Width}x{emo.Height})");
}

// 收藏表情包
await client.CollectEmoticonAsync(
    imageId: emoticons.Items[0].Id,
    isAdd: true,
    imageUrl: emoticons.Items[0].Url,
    imageWidth: emoticons.Items[0].Width,
    imageHeight: emoticons.Items[0].Height,
    cancellationToken: cts.Token
);

// 评论配图上传（使用标准 Multipart 协议直接上传本地图片流）
using var fileStream = File.OpenRead("photo.jpg");
UploadedImage uploaded = await client.UploadPictureAsync(fileStream, "photo.jpg", "image/jpeg", cts.Token);
Console.WriteLine($"图片上传成功，ImageId: {uploaded.ImageId}, WebUri: {uploaded.WebUri}");
```

---

### 11. 负反馈与不感兴趣推荐过滤

```csharp
// 获取客户端负反馈长按选项卡（卡片类型 2 为短剧负反馈）
DislikePanel panel = await client.GetDislikePanelAsync(cardType: 2, cts.Token);
foreach (DislikeGroup group in panel.Groups)
{
    Console.WriteLine($"分组: {group.Title}");
    foreach (DislikeOption opt in group.Options)
    {
        Console.WriteLine($"  - [{opt.Id}] {opt.Text} (Head: {opt.Head})");
    }
}

// 提交不感兴趣反馈（按演员、分类或剧集屏蔽）
DislikeOption chosenOption = panel.Groups[0].Options[0];
bool success = await client.SubmitDislikeAsync(
    dramaId: "7699833115866303550",
    videoId: "7699833115866303551",
    recommendInfo: "",
    option: chosenOption,
    cancellationToken: cts.Token
);
```

---

### 12. 跨页去重流式遍历

对于推荐流、题材分类库或排行榜，客户端提供了基于 `IAsyncEnumerable<T>` 的流式遍历接口。底层自动维护已访问 ID 集合以实现跨页去重，并支持随时取消：

```csharp
// 流式消费真人短剧推荐，跨页自动去重
await foreach (Drama drama in client.EnumerateRecommendationsAsync(genre: RecommendationGenre.ShortDrama, cancellationToken: cts.Token))
{
    Console.WriteLine($"流式接收: 《{drama.Title}》 (ID: {drama.Id})");
    // 处理达到数量或条件后直接 break 退出
}
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
