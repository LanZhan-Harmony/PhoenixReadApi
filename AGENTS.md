# PhoenixReadApi 项目开发规范

本文档是 `D:\code\C#\PhoenixReadApi` 的项目级开发规范。修改代码、公共 API、协议 DTO、示例、测试或打包配置前，应先遵循本文档。

## 1. 项目定位与架构

PhoenixReadApi 是面向 **.NET 11 (`net11.0`) / C# 15** 的红果短剧 API NuGet 客户端，兼容 **NativeAOT** 与裁剪发布。唯一公共入口为 `PhoenixReadApiClient`（按领域拆分为 `PhoenixReadApiClient.[Domain].cs` partial 文件，严禁在门面编写业务逻辑或用空继承类制造别名）。

依赖方向严格保持单向：

```text
Public API (PhoenixReadApiClient / Public/*)
    ↓
Features ([Domain]Service)
    ↓
Infrastructure / Serialization
    ↓
Contracts (App / Web / Shared)
```

- **Public (`Public/Enums`, `Public/Exceptions`, `Public/Models/[Domain]/`)**：稳定的领域模型。**物理按单数领域目录归类，但 C# `namespace` 必须保持根命名空间 `PhoenixReadApi`**。
- **Features (`Features/[Domain]/`)**：内部服务 `[Domain]Service`（`PhoenixReadApi.Internal.Features.[Domain]`），负责参数校验、协议编排与模型映射。超过 300～400 行或职责混杂时按子能力拆分；禁止使用 `Manager`、`Helper`、`Processor` 等泛化命名。
- **Contracts (`Contracts/[App|Web|Shared]/[Domain]/`)**：外部协议 DTO（`internal sealed`）。App 接口加 `App` 前缀，Web 接口加 `Web` 前缀；请求体用 `Payload` 后缀，响应体用 `Data`/`Response`/页面名后缀（禁用含糊的 `Result`）。通常一类一文件，严禁重建 `Models.cs`、`Payloads.cs` 大杂烩。
- **Infrastructure (`Infrastructure/[Http|Crypto|Web|Runtime]/`)**：`internal`，统一处理传输、超时、重试、签名、HTML 提取与会话状态。

## 2. 命名规范与领域词典

1. **单数优先**：所有领域目录、命名空间、服务类和门面分部文件一律使用单数（如 `Comment`, `Drama`, `Favorite`, `Follow`, `Interaction`, `Library`, `Notification`, `Playback`, `Search`, `Share`, `User`），严禁单复数混用。仅集合属性使用复数（如 `Items`, `Tags`, `Credits`）。
2. **禁止任意缩写**：认证用 `Authentication`（禁用 `Auth`），筛选用 `ShortDramaFilter`，负反馈用 `Interaction`，分享用 `Share`。
3. **概念唯一**：同一领域概念全局统一命名，屏蔽服务端方言字段：

| 语义 | 标准命名 | 禁用别名 |
| :--- | :--- | :--- |
| 短剧 ID | `Drama.Id` / `DramaId` | `SeriesId`, `BookId`, `NovelBookId` |
| 分集/短视频 ID | `VideoId` | `EpisodeId`, `VideoUrl`, `ItemId` |
| 关联原著小说 ID | `NovelId` | `NovelBookId`, `BookId` |
| 点赞数 | `LikeCount` | `DiggCount`, `Likes` |
| 列表/分页项集合 | `Items` | `Books`, `Dramas`, `List`, `Data` |
| 毫秒偏移 / 绝对时间 | `OffsetMs` / `*UnixMilliseconds` / `*UnixSeconds` | `TimeMs`, `Time`, `Timestamp` |
| 剧评 / 评论 | `DramaReview` / `Comment` | `DramaComment`, `CommentItem` |
| 历史记录 / 批量删除 | `HistoryItem<T>` / `HistoryPage<T>` / `HistoryDeleteEntry` | `ReadHistoryEntry`, `ReadHistoryPage` |
| 筛选维度 / 选项 | `FilterGroup` / `FilterItem` | `FilterRow`, `FilterOption` |
| 认证用户 | `AuthenticatedUser` | `CurrentUserInfo`, `AppCurrentUserInfo` |

4. **多态与目标描述符**：同一接口返回多种实体时优先使用 C# 15 `public readonly union`（如 `VideoSearchItem`, `UserWork`, `RelatedWorkItem`, `RecommendationFeedItem`, `CircleItem`）；跨多种目标的统一操作使用轻量描述符（如 `CommentTarget`、`HistoryDeleteEntry`）配合 `From*` 静态工厂方法，避免方法数量按“目标 × 动作”笛卡尔积膨胀。
5. **命名空间遮蔽处理**：当 `PhoenixReadApi.Internal.Features.[Domain]` 与根空间模型同名导致 `CS0118` 时，**必须在文件范围 `namespace ...;` 声明之后**添加显式别名（如 `using Drama = PhoenixReadApi.Drama;`），禁止写在 `namespace` 之前，也禁止把公共类改名为 `DramaModel`。

## 3. 编码与序列化边界

- **语言规范**：遵守 `.editorconfig`（4 空格、CRLF、文件范围命名空间、Allman 大括号、显式访问修饰符）；新类型默认 `sealed`；公共成员必须有 XML 文档；异步方法以 `Async` 结尾并将 `CancellationToken` 置于末尾且透传到底层；集合返回 `IReadOnlyList<T>` / `IReadOnlyDictionary<TKey, TValue>`；禁止 `async void`、阻塞 `.Result`/`.Wait()` 及库内 `Console.WriteLine`。
- **NativeAOT 与 JSON 边界**：
  - 所有库内 JSON 序列化/反序列化必须登记在 `Serialization/JsonAotSerializer.cs` 的 `JsonSerializerContext` 中，禁止正常路径使用反射式 `JsonSerializer`。
  - 数据流：`HTTP bytes → SourceGenerationContext DTO → JsonConverter（兼容不稳定结构）→ Feature Mapper → Public Model`。
  - `JsonDocument`/`JsonElement` 仅允许用于 HTML（`_ROUTER_DATA` / JSON-LD）提取器、`JsonConverter` 内部及无法预描述的动态节点；Feature 服务中严禁出现 `TryGetProperty`、`EnumerateArray`、`GetRawText` 等逐字段读取或通用 `JsonHelpers`。
  - 迁移完成后及时清理无调用方的旧 DTO 与 AOT 注册。

## 4. HTTP、认证与异常

- **HttpClient 与超时**：外部注入的 `HttpClient` 由调用方管理，库内不新建 `HttpClient` 也不篡改全局默认头；`RequestTimeout` 覆盖整个请求（含响应头与响应体读取）；超限响应抛出 `PhoenixReadApiResponseException`；取消令牌原样传播。
- **重试策略**：使用 `AppRequestRetryMode` 显式声明：`ReadOnly` 可重试，`IdempotentWrite` 仅限确认幂等写接口，`NoRetry` 用于短信、登录、评论、回复、点赞、收藏、关注、预约、进度上报等副作用操作。禁止通过 `path.Contains(...)` 猜测策略。
- **认证与敏感数据**：`AuthenticationStateFilePath` 与 `AnonymousStateFilePath` 互斥；Cookie、Token、Passport 头、短信票据及完整认证响应严禁写入日志、异常消息、README、测试输出或提交文件；`Logout()` 须同时清理内存与本地快照；设备 ID 默认随机生成。
- **异常模型**：公开异常统一继承 `PhoenixReadApiException`（`Transport` / `Http` / `Provider` / `Deserialization` / `Response` / `Media` / `Fallback` / `Authentication`），将状态码、操作名、URI、错误码及 `IsTransient` 写入属性；参数校验直接抛出标准 `ArgumentException` / `ArgumentNullException` / `ArgumentOutOfRangeException`。

## 5. 映射、测试与发布检查

- **数据校验**：Mapper 只做 DTO → 公开模型转换，不发请求；必须校验/归一化 ID、集数归属、URL、游标和时间戳；清晰度仅接受明确标识（`360p`/`720p`/`1080p`），不得把 URL/ID 中的任意数字当成清晰度。
- **测试要求**：优先使用基于 `HttpMessageHandler` 的确定性单元测试覆盖协议映射、边界校验、重试与取消；端到端真机测试中产生的写操作数据（弹幕、评论、回复、点赞、收藏等）必须在测试结束后及时清理。示例项目 `PhoenixReadApi.Sample` 仅调用稳定公开 API。
- **构建与打包**：任何公共 API 变更须同步检查 XML 文档、`README.md`、`PhoenixReadApi.Sample` 与 `PhoenixReadApi.Tests`，并确保 `PhoenixReadApi` Release 构建满足 `GenerateDocumentationFile`、`EnablePackageValidation`、`IsAotCompatible` 零警告零错误。未经用户明确要求，不要擅自修改版本号、发布 NuGet 或提交远程仓库。
