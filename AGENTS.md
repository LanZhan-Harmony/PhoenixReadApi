# PhoenixReadApi 项目开发规范

本文档是 `D:\code\C#\PhoenixReadApi` 的项目级开发规范。修改本仓库中的代码、公共 API、协议 DTO、示例、测试或打包配置前，应先阅读本文档。子目录可以添加更具体的 `AGENTS.md`，但不能降低本文档对公共 API、数据安全和序列化边界的要求。

## 1. 项目定位

PhoenixReadApi 是一个面向 .NET 10 的红果短剧 API NuGet 客户端。它同时访问 App 接口和网页接口，并把不稳定的服务端协议转换为稳定的公开模型。

当前项目是单程序集、内部模块化架构：

- `PhoenixReadApi`：NuGet 类库；
- `PhoenixReadApi.Sample`：手工演示和端到端场景示例；
- `PhoenixReadApi.Tests`：测试项目；
- `README.md`：包说明和公开 API 使用示例；
- `PhoenixReadApi.slnx`：解决方案入口。

库的公共入口当前是 `PhoenixReadApiClient`。除非用户明确要求进行破坏性重命名，不要擅自同时引入第二个公共入口类，也不要用空继承包装类制造别名。

## 2. 总体架构

依赖方向应保持为：

```text
Public API
    ↓
Features
    ↓
Infrastructure / Serialization
    ↓
Contracts
```

具体目录职责如下：

```text
PhoenixReadApi/
├── Public/                  # 稳定的用户可见类型
│   ├── Enums/
│   ├── Exceptions/
│   └── Models/
├── Features/                # 按业务能力组织的内部服务
│   ├── Catalog/
│   ├── Details/
│   ├── Search/
│   ├── Playback/
│   ├── Danmaku/
│   ├── Comments/
│   ├── Users/
│   ├── Authentication/
│   ├── Library/
│   └── Shared/
├── Contracts/               # 外部协议 DTO 和请求 Payload
│   ├── App/
│   ├── Web/
│   └── Shared/
├── Infrastructure/
│   ├── Http/
│   ├── Runtime/
│   ├── Crypto/
│   └── Web/
└── Serialization/           # AOT 上下文、Converter、值规范化
```

### 2.1 公共门面

`PhoenixReadApiClient` 只负责：

- 保存运行时和功能服务实例；
- 暴露稳定的异步公共方法；
- 转发参数和取消令牌；
- 释放由客户端拥有的资源。

它不应包含：

- HTTP 组装；
- JSON 字段读取；
- 服务端字段兼容逻辑；
- 签名算法；
- 分页合并算法；
- 业务模型映射。

公共门面可以使用多个 partial 文件按领域拆分，例如 `PhoenixReadApiClient.Search.cs`、`PhoenixReadApiClient.Comments.cs`。partial 只是文件组织方式，不能把业务逻辑重新放回门面。

### 2.2 Feature 服务

每个 Feature 服务负责一个业务边界。服务之间通过公开模型、内部 DTO 或共享的内部辅助类协作，不直接互相调用底层 HTTP 细节。

当服务超过约 300～400 行，或同时处理查询、写操作、映射、分页和缓存时，应优先拆分职责。例如：

- 搜索拆为搜索页、联想、推荐、季数补齐和合并逻辑；
- 评论拆为查询、评论写操作和回复；
- Library 拆为历史/进度、收藏、点赞、关注、预约和发现；
- 用户拆为主页聚合、内容分页和映射。

服务名称使用领域名称，不使用 `Manager`、`Helper` 这类无法说明职责的泛化名称。

### 2.3 Infrastructure

Infrastructure 处理协议和运行时机制，不暴露给 NuGet 用户：

- `Http`：请求发送、响应读取、超时、重试、会话头；
- `Crypto`：签名、Base64、媒体密钥和协议加密；
- `Web`：HTML、`_ROUTER_DATA` 和 JSON-LD 提取；
- `Runtime`：单个客户端实例共享的传输和配置状态。

Infrastructure 类型使用 `internal`，命名空间以 `PhoenixReadApi.Internal.Infrastructure.*` 开头。

## 3. C# 编码规范

- 目标框架保持 `net10.0`，开启 nullable reference types 和 NativeAOT 兼容性。
- 遵守根目录 `.editorconfig`：4 空格缩进、CRLF、文件范围命名空间、Allman 大括号、显式可访问性修饰符。
- 类型、方法、属性和事件使用 PascalCase；局部变量和参数使用 camelCase；私有字段使用 `_camelCase`。
- 新类型默认使用 `sealed`，除非确实需要被继承；公共继承关系必须有明确的扩展场景。
- 公共类型和公共成员必须有 XML 文档；内部类型只在协议兼容逻辑复杂时添加必要说明。
- 使用 `ArgumentNullException.ThrowIfNull`、`ArgumentOutOfRangeException` 和明确的参数名称。
- 异步方法使用 `Async` 后缀并接受 `CancellationToken`；取消令牌应一路传到 HTTP 和响应读取层。
- 不使用 `async void`，事件处理以外不得使用阻塞式 `.Result`、`.Wait()` 或同步网络调用。
- 集合返回值优先使用 `IReadOnlyList<T>` 或 `IReadOnlyDictionary<TKey,TValue>`；不要把内部可变集合直接暴露给调用方。
- 时间字段必须在名称或 XML 文档中明确单位和时区，例如 `UpdatedAtUnixMilliseconds`。
- 不在库代码中使用 `Console.WriteLine` 输出诊断；诊断通过异常元数据或调用方注入的 `HttpClient`/处理器完成。
- 不提交 `bin/`、`obj/`、临时会话文件、Cookie、Token、手机号、真实账号信息或抓取到的敏感响应。

## 4. 公共 API 设计

### 4.1 公共模型与协议 DTO 分离

`Public/Models` 是给用户使用的稳定领域模型，不是服务端 JSON 的镜像。服务端字段名、兼容字段和请求协议不得直接泄露到公共模型。

公共模型应表达领域语义：

- 剧集使用 `Drama.Id` 作为红果剧集 ID；不要重新引入 `Source`、`SourceId`、`Channel` 或 `DisplayTitle`；
- 剧集的一集使用 `Episode`，集数属性使用 `Number`；
- `Episode.VideoId` 表示视频 ID，不要命名为 `VideoUrl`；
- 状态、推荐题材和搜索建议类型使用枚举，不把服务端字符串直接暴露给用户；
- 分页模型明确区分 `Cursor`、`NextOffset`、`HasMore` 和 `Total`；
- `Referer`、CENC 密钥等播放协议信息只有在调用方实际需要时才放在播放结果中，不放进剧集元数据。

新增公共属性前必须回答：

1. 这是用户稳定需要的领域信息，还是当前服务端协议的偶然字段？
2. 它的命名是否与红果之外的领域语义一致？
3. 后端字段变化时，库能否继续保持该属性的含义？
4. 是否可以通过已有模型或枚举表达，而不增加重复字段？

公共 API 一旦发布就视为兼容承诺。删除、重命名、改变类型或改变默认值属于破坏性变更，应在主版本升级中进行；尚未发布时可以一次性清理，不要用长期空壳类型掩盖错误设计。

### 4.2 参数和默认值

- 数量、偏移量、游标、ID、评分和文本都在公共入口处或 Feature 边界处校验。
- 默认值应代表安全、常用且稳定的行为；服务端内部限制不能无说明地暴露为魔法数字。
- 如果调用方确实需要控制结果数量，提供明确参数并设有上限；缓存键必须包含会影响结果的参数。
- `CancellationToken` 放在参数列表末尾。
- 对复杂分页请求优先使用请求模型，例如 `RecommendationQuery`、`CategoryQuery`，避免继续增加长参数列表。
- 语义别名只在确实改善可发现性或兼容旧 API 时保留；新 API 不要继续增加重复别名。

## 5. Contracts、DTO 和 Payload 规范

### 5.1 目录和命名

- 所有协议类型使用 `internal sealed`，除非源代码生成或框架要求其他形式。
- 新文件通常一个主要类型一个文件；相关的极小嵌套 DTO 可以放在同一文件，但不得重新建立 `Models.cs`、`Payloads.cs` 这种无边界大文件。
- 物理目录与命名空间保持一致，例如：

  ```text
  Contracts/App/Details/AppDetailsData.cs
  PhoenixReadApi.Internal.Contracts.App.Details
  ```

- App DTO 只能描述 App 接口；Web DTO 只能描述网页接口；共享字段才放在 `Contracts/Shared`。
- 评论动作和回复 DTO 放在 `Contracts/App/Comment` 下；Library DTO 不得混入评论协议。
- 请求对象使用 `Payload` 后缀，响应对象使用 `Data`、`Response` 或明确的页面名称；不要使用含糊的 `Result` 作为协议 DTO 名称。

### 5.2 字段映射

- 服务端字段使用 `[JsonPropertyName]` 明确映射，不依赖命名策略猜测关键字段。
- 类型不稳定的字符串、整数、列表或对象通过局部 `JsonConverter` 兼容，不在 Feature 服务中手动读取字段。
- DTO 可以包含服务端的多个兼容字段，但映射器必须把它们归一为一个稳定的领域值。
- 不把公开模型直接登记为后端响应 DTO；公开模型由 Mapper 构造。
- 不把 `JsonElement` 当作业务层的通用逃生舱。只有在协议本身确实是动态结构时，才允许在 Converter 或边界适配器内部使用。

## 6. JSON、AOT 和序列化边界

项目必须保持 NativeAOT 和裁剪兼容：

- 所有库内 JSON 类型都登记在 `Serialization/JsonAotSerializer.cs` 的 source-generated `JsonSerializerContext` 中。
- 禁止在库的正常路径中依赖反射式 `JsonSerializer.Serialize/Deserialize<T>`。
- `JsonDocument`/`JsonElement` 的允许边界只有：
  - `_ROUTER_DATA` 或 JSON-LD 的 HTML 提取器；
  - `JsonConverter` 内部处理服务端不稳定结构；
  - 无法预先描述的动态协议对象，并且必须立即转换为内部 DTO 或公开模型。
- Feature 服务不得出现 `TryGetProperty`、`EnumerateArray`、`GetRawText` 这类字段级 JSON 读取逻辑。
- 不重新创建 `JsonHelpers`、`JsonValueReader` 一类通用 JSON 读取器。已反序列化对象的非空选择、ID 校验和格式化逻辑使用 `ValueNormalizer` 等语义明确的工具。
- 新增 Converter 时说明它兼容的服务端形态，并限制输入大小、递归深度和异常行为。
- 不为暂时没有调用方的旧 Converter 或 DTO 保留 source-generation 注册；完成迁移后删除死代码。

推荐的数据流为：

```text
HTTP bytes
  → SourceGenerationContext DTO
  → Converter / 协议校验
  → Feature Mapper
  → Public Model
```

## 7. HTTP、超时和重试

### 7.1 HttpClient 生命周期

- 默认构造函数创建并拥有 `HttpClient`；注入的 `HttpClient` 始终由调用方管理。
- 不在 Feature 服务中创建新的 `HttpClient`。
- 不在库中偷偷修改调用方 `HttpClient` 的全局默认配置。
- 所有请求都经过统一的 HTTP transport，以便统一处理响应大小、超时、请求 URI 和异常元数据。

### 7.2 超时

- `RequestTimeout` 应表达整个请求的截止时间，包括响应头和响应体读取；后续修改不得让两个阶段各自重新获得完整超时时间。
- 响应体必须有最大字节数限制；超限抛出 `PhoenixReadApiResponseException`。
- 调用方取消应原样传播，不要把主动取消包装成超时。

### 7.3 重试

使用 `AppRequestRetryMode` 明确表达请求语义：

- `ReadOnly`：读取接口可以按配置重试；
- `IdempotentWrite`：确定服务端幂等的写入接口才可重试；
- `NoRetry`：短信、登录、评论、回复、点赞、收藏、关注、预约、进度上报等可能产生副作用的操作只发送一次。

新增写接口必须显式指定重试模式，不能依赖默认值。不要通过 `path.Contains(...)` 猜测请求是否需要签名或重试；请求策略应是显式参数或专用方法。

重试异常必须保留最后一个有意义的内部异常，并设置 `IsTransient`。不要对参数错误、JSON 结构错误或业务校验失败进行重试。

## 8. 认证、会话和敏感数据

- `AuthenticationStateFilePath` 与 `AnonymousStateFilePath` 不能同时设置；配置冲突应立即抛出异常。
- 默认设备 ID、安装 ID 和会话标识必须随机生成；只有调用方显式传入或从同一会话快照恢复时才复用。
- Cookie、Token、Passport 头、短信票据和完整认证响应都属于敏感数据。
- 不在日志、异常消息、README、测试输出或提交文件中输出敏感值。
- 认证响应诊断默认关闭；如确实需要诊断，必须脱敏 Cookie、Token、手机号和响应体，并限制文件位置和生命周期。
- 会话快照应使用原子写入；恢复失败只能影响会话恢复，不能阻止匿名读取功能启动。
- `Logout()` 必须清除内存会话和持久化快照中的认证数据。
- 恢复用户资料时，`AuthenticationState.User` 必须与持久化会话状态一致；没有用户资料时不要声称已恢复完整登录身份。
- 认证 transport、普通 App transport 和会话存储应保持可拆分，避免继续把所有职责集中在一个类中。

## 9. 异常模型

公开异常统一继承 `PhoenixReadApiException`，并保留可供调用方处理的元数据：

```text
PhoenixReadApiException
├── PhoenixReadApiTransportException
├── PhoenixReadApiHttpException
├── PhoenixReadApiProviderException
├── PhoenixReadApiDeserializationException
├── PhoenixReadApiResponseException
├── PhoenixReadApiMediaException
├── PhoenixReadApiFallbackException
└── PhoenixReadApiAuthenticationException
```

- JSON 无法解析使用 `DeserializationException`；JSON 合法但缺少业务所需结构使用 `ResponseException`。
- HTTP 状态、逻辑操作、请求 URI、提供方错误码和是否可重试应尽量写入异常属性，而不是只拼到消息字符串中。
- 不把 Cookie、Token、完整响应体或用户输入直接放入异常消息。
- 兜底失败如果有多个来源，应保留来源和异常关系；不要只保留最后一个失败文本。
- 参数错误使用 .NET 标准参数异常，不包装为 API 远程异常。

## 10. 映射、校验和数据质量

- Mapper 负责 DTO → 公开模型，不负责发请求。
- Mapper 必须对 ID、集数、分集归属、媒体 URL、分页游标和重复项进行校验或归一化。
- 无法识别的服务端记录应跳过并记录有限诊断；不能因为一条坏记录让整页有效数据失败，除非响应整体违反安全或一致性约束。
- 合并剧集以 `Drama.Id` 去重；合并时保留已有非空字段，并合并标签、署名等集合。
- 清晰度解析只接受明确的清晰度标识，例如 `360p`、`720p`、`1080p`、`2160p`；不能把媒体 URL 或视频 ID 中的任意数字当作清晰度。
- 时间戳、游标、ID 和 URL 必须限制长度，并拒绝控制字符、换行和明显不合法的值。

## 11. 测试和示例

测试应优先验证稳定的协议和业务行为，而不是依赖实时站点的偶然结果：

- Converter、Mapper、ID/分页/文本校验使用确定性测试；
- HTTP 行为使用可控的 `HttpMessageHandler`，覆盖状态码、超时、响应过大、重试和取消；
- App/Web 兜底使用模拟响应验证调用顺序和异常聚合；
- 真实网络测试作为端到端测试，必须容忍服务端数据变化，不应成为唯一测试形式；
- NativeAOT 相关改动需要检查 source-generation 注册和裁剪可用性；
- 示例项目只展示稳定公共 API，不直接引用 `Internal` 类型，不依赖开发机上的 Cookie、Token 或固定账号。

不要为了提高覆盖率测试内部实现细节；优先测试公共方法的输入、输出、异常和副作用。

## 12. 文档、版本和打包

任何公共 API 变化都要同步检查：

- XML 文档；
- `README.md` 快速开始和相关示例；
- `PhoenixReadApi.Sample`；
- `PhoenixReadApi.Tests`；
- 包描述、标签和 Release Notes；
- 版本兼容性和迁移说明。

NuGet 包必须继续满足：

- `GenerateDocumentationFile`；
- `EnablePackageValidation`；
- SourceLink；
- NativeAOT 兼容性；
- source-generated JSON；
- 不把内部 DTO 或实现类型作为公共包契约暴露。

版本策略：

- 修复实现且不改变公共契约：补丁版本；
- 新增向后兼容公共 API：次版本；
- 删除、重命名、改类型或改变破坏性默认行为：主版本；
- 尚未正式发布的 API 可以直接清理，但要一次性同步所有引用和文档。

## 13. 变更流程

开始修改前：

1. 搜索相关公共类型、DTO、Mapper、Feature 和 README 示例的全部引用；
2. 判断修改属于公共契约、协议兼容层还是内部实现；
3. 明确是否需要新增 DTO、Converter、异常或枚举；
4. 确认是否会影响 NativeAOT、重试、会话安全或 NuGet 兼容性。

修改过程中：

1. 保持职责边界，不把临时解析逻辑塞回大型服务；
2. 先修改契约和映射，再修改 Feature 调用；
3. 删除迁移后的死代码、旧命名和无调用方的 source-generation 注册；
4. 同步更新 XML 文档、README、Sample 和相关测试。

完成后：

1. 检查 `rg` 是否仍有旧类型名、旧字段名、错误命名空间或 `JsonElement` 业务读取；
2. 检查公共 API 是否意外暴露了内部协议字段；
3. 检查异常、重试和取消行为；
4. 按用户要求进行构建、测试或 NativeAOT 验证，并在交付说明中明确实际执行过的验证。

## 14. 明确禁止的做法

- 新建集中所有模型的 `Models.cs`、所有请求的 `Payloads.cs` 或不受控的 `Helpers.cs`；
- 在 Feature 服务里用 `JsonElement` 逐字段解析正常 DTO 响应；
- 用路径字符串判断请求策略、签名方式或是否安全重试；
- 对非幂等写操作使用默认自动重试；
- 把 App/Web 协议 DTO 暴露给 NuGet 用户；
- 把 `Source`、`SourceId`、`Channel` 等单数据源冗余信息重新加入公开剧集模型；
- 在日志、测试、异常或诊断文件中泄露 Cookie、Token、手机号或完整认证响应；
- 用空包装继承类长期维护重复公共入口；
- 为了“兼容”而保留没有调用方、没有文档、没有测试覆盖的死代码；
- 未经用户要求修改包版本、发布 NuGet、发送外部消息或提交远程仓库。
