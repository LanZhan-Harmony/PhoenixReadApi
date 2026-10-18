namespace PhoenixReadApi;

/// <summary>带分页状态的剧集列表。</summary>
public sealed class DramaPage
{
    /// <summary>当前页的剧集。</summary>
    public IReadOnlyList<Drama> Items { get; init; } = [];

    /// <summary>服务端报告的总数。</summary>
    public long Total { get; init; }

    /// <summary>下一次偏移量。</summary>
    public int NextOffset { get; init; }

    /// <summary>下一页游标。</summary>
    public string Cursor { get; init; } = string.Empty;

    /// <summary>是否还有下一页。</summary>
    public bool HasMore { get; init; }
}

/// <summary>阅读历史中的一条短剧记录。</summary>
public sealed class ReadHistoryEntry
{
    /// <summary>服务端记录 ID。</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>剧集 ID。</summary>
    public string SeriesId { get; init; } = string.Empty;

    /// <summary>视频 ID。</summary>
    public string VideoId { get; init; } = string.Empty;

    /// <summary>集数。</summary>
    public int EpisodeNumber { get; init; }

    /// <summary>剧集标题。</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>封面 URL。</summary>
    public string CoverUrl { get; init; } = string.Empty;

    /// <summary>最近播放位置（毫秒）。</summary>
    public long CurrentPositionMs { get; init; }

    /// <summary>视频时长（毫秒）。</summary>
    public long DurationMs { get; init; }

    /// <summary>最近阅读时间（Unix 毫秒）。</summary>
    public long UpdatedAtUnixMilliseconds { get; init; }
}

/// <summary>阅读历史分页结果。</summary>
public sealed class ReadHistoryPage
{
    /// <summary>当前页记录。</summary>
    public IReadOnlyList<ReadHistoryEntry> Items { get; init; } = [];

    /// <summary>服务端报告的总数。</summary>
    public long Total { get; init; }

    /// <summary>下一次偏移量。</summary>
    public int NextOffset { get; init; }

    /// <summary>是否还有下一页。</summary>
    public bool HasMore { get; init; }
}

/// <summary>一条短剧播放进度。</summary>
public sealed class ReadProgress
{
    /// <summary>剧集 ID。</summary>
    public string SeriesId { get; init; } = string.Empty;

    /// <summary>视频 ID。</summary>
    public string VideoId { get; init; } = string.Empty;

    /// <summary>当前集数。</summary>
    public int EpisodeNumber { get; init; }

    /// <summary>总集数。</summary>
    public int EpisodeCount { get; init; }

    /// <summary>最近观看时间（Unix 毫秒）。</summary>
    public long LastWatchTimeUnixMilliseconds { get; init; }

    /// <summary>当前播放位置（毫秒）。</summary>
    public long CurrentPositionMs { get; init; }

    /// <summary>当前视频总时长（毫秒）。</summary>
    public long DurationMs { get; init; }

    /// <summary>累计播放时长（毫秒）。</summary>
    public long CumulativeDurationMs { get; init; }
}

/// <summary>播放进度查询结果。</summary>
public sealed class ReadProgressPage
{
    /// <summary>服务端返回的播放进度。</summary>
    public IReadOnlyList<ReadProgress> Items { get; init; } = [];

    /// <summary>本地书籍进度；短剧通常为空。</summary>
    public IReadOnlyList<ReadProgress> LocalItems { get; init; } = [];
}

/// <summary>当前账号关注动态中的一条记录。</summary>
public sealed class FollowUpdate
{
    /// <summary>动态或视频 ID。</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>发布者 ID。</summary>
    public string UserId { get; init; } = string.Empty;

    /// <summary>动态文字。</summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>封面 URL。</summary>
    public string CoverUrl { get; init; } = string.Empty;

    /// <summary>创建时间（Unix 秒）。</summary>
    public long CreatedAtUnixSeconds { get; init; }

    /// <summary>关联剧集；服务端未提供时为空对象。</summary>
    public Drama Drama { get; init; } = new();
}

/// <summary>关注动态分页结果。</summary>
public sealed class FollowUpdatePage
{
    /// <summary>当前页动态。</summary>
    public IReadOnlyList<FollowUpdate> Items { get; init; } = [];

    /// <summary>下一页游标。</summary>
    public string Cursor { get; init; } = string.Empty;

    /// <summary>是否还有下一页。</summary>
    public bool HasMore { get; init; }

    /// <summary>服务端报告的总数。</summary>
    public long Total { get; init; }
}

/// <summary>一条预约记录。</summary>
public sealed class Reservation
{
    /// <summary>预约记录 ID。</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>剧集 ID。</summary>
    public string SeriesId { get; init; } = string.Empty;

    /// <summary>视频 ID。</summary>
    public string VideoId { get; init; } = string.Empty;

    /// <summary>剧集标题。</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>封面 URL。</summary>
    public string CoverUrl { get; init; } = string.Empty;

    /// <summary>预约时间（Unix 秒）。</summary>
    public long ReservedAtUnixSeconds { get; init; }

    /// <summary>预计更新时间（Unix 秒）。</summary>
    public long ReleaseAtUnixSeconds { get; init; }

    /// <summary>预约是否仍然有效。</summary>
    public bool IsActive { get; init; }
}

/// <summary>预约列表分页结果。</summary>
public sealed class ReservationPage
{
    /// <summary>当前页预约记录。</summary>
    public IReadOnlyList<Reservation> Items { get; init; } = [];

    /// <summary>下一次偏移量。</summary>
    public int NextOffset { get; init; }

    /// <summary>是否还有下一页。</summary>
    public bool HasMore { get; init; }

    /// <summary>服务端报告的总数。</summary>
    public long Total { get; init; }
}

/// <summary>分类筛选项。</summary>
public sealed class CategoryOption
{
    /// <summary>分类或筛选项 ID。</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>显示名称。</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>服务端导航地址。</summary>
    public string Schema { get; init; } = string.Empty;
}

/// <summary>分类页返回的筛选面板和剧集。</summary>
public sealed class CategoryPage
{
    /// <summary>当前页剧集。</summary>
    public IReadOnlyList<Drama> Items { get; init; } = [];

    /// <summary>可用筛选项。</summary>
    public IReadOnlyList<CategoryOption> Options { get; init; } = [];

    /// <summary>下一页偏移量。</summary>
    public int NextOffset { get; init; }

    /// <summary>分页会话 ID。</summary>
    public string SessionId { get; init; } = string.Empty;

    /// <summary>是否还有下一页。</summary>
    public bool HasMore { get; init; }
}

/// <summary>一个剧集关联的分季或相关推荐记录。</summary>
public sealed class RelatedDrama
{
    /// <summary>剧集信息。</summary>
    public Drama Drama { get; init; } = new();

    /// <summary>在关联列表中的顺序。</summary>
    public int Order { get; init; }

    /// <summary>关联关系说明。</summary>
    public string Relation { get; init; } = string.Empty;
}

/// <summary>分享链接及其对应的剧集和分集。</summary>
public sealed class ShareLink
{
    /// <summary>完整分享 URL。</summary>
    public string Url { get; init; } = string.Empty;

    /// <summary>短链接。</summary>
    public string ShortUrl { get; init; } = string.Empty;

    /// <summary>剧集 ID。</summary>
    public string SeriesId { get; init; } = string.Empty;

    /// <summary>视频 ID。</summary>
    public string VideoId { get; init; } = string.Empty;

    /// <summary>分享标题。</summary>
    public string Title { get; init; } = string.Empty;
}

/// <summary>一个无返回数据的操作结果。</summary>
public sealed class PhoenixOperationResult
{
    /// <summary>服务端是否报告成功。</summary>
    public bool Succeeded { get; init; }

    /// <summary>服务端返回的消息。</summary>
    public string Message { get; init; } = string.Empty;
}

/// <summary>上传播放进度时使用的参数。</summary>
public sealed class ProgressUpdate
{
    /// <summary>视频 ID。</summary>
    public string VideoId { get; init; } = string.Empty;

    /// <summary>所属剧集 ID。</summary>
    public string SeriesId { get; init; } = string.Empty;

    /// <summary>当前播放位置（毫秒）。</summary>
    public long CurrentPositionMs { get; init; }

    /// <summary>视频总时长（毫秒）。</summary>
    public long DurationMs { get; init; }

    /// <summary>最近观看时间（Unix 毫秒）；默认使用当前时间。</summary>
    public long UpdatedAtUnixMilliseconds { get; init; }

    /// <summary>是否把这条记录标记为删除。</summary>
    public bool UseSoftDelete { get; init; }
}

/// <summary>分类筛选请求。</summary>
public sealed class CategoryQuery
{
    /// <summary>剧集分类。</summary>
    public PhoenixReadApiCategory Category { get; init; } = PhoenixReadApiCategory.All;

    /// <summary>服务端分类主题 ID。</summary>
    public IReadOnlyList<string> ThemeIds { get; init; } = [];

    /// <summary>角色筛选 ID。</summary>
    public IReadOnlyList<string> RoleIds { get; init; } = [];

    /// <summary>年代筛选 ID。</summary>
    public IReadOnlyList<string> EpochIds { get; init; } = [];

    /// <summary>上线时间筛选 ID。</summary>
    public IReadOnlyList<string> OnlineTimeIds { get; init; } = [];

    /// <summary>更新状态筛选 ID。</summary>
    public IReadOnlyList<string> StatusIds { get; init; } = [];

    /// <summary>请求偏移量。</summary>
    public int Offset { get; init; }

    /// <summary>分页会话 ID。</summary>
    public string SessionId { get; init; } = string.Empty;
}
