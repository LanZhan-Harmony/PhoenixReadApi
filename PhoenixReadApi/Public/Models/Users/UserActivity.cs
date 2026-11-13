namespace PhoenixReadApi;

/// <summary>账号公开动态中的一条视频内容。</summary>
public sealed class UserActivity
{
    /// <summary>动态关联的视频 ID。</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>动态文字或视频标题。</summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>动态封面 URL。</summary>
    public string CoverUrl { get; init; } = string.Empty;

    /// <summary>创建时间的 Unix 时间戳（秒）。</summary>
    public long CreatedAtUnixSeconds { get; init; }

    /// <summary>获赞数。</summary>
    public long LikeCount { get; init; }

    /// <summary>播放数。</summary>
    public long ViewCount { get; init; }
}
