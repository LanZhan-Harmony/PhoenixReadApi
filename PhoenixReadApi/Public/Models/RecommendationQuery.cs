namespace PhoenixReadApi;

/// <summary>推荐接口的输入游标。</summary>
public sealed class RecommendationQuery
{
    /// <summary>推荐题材。</summary>
    public string Genre { get; init; } = string.Empty;

    /// <summary>分页偏移量。</summary>
    public int Offset { get; init; }

    /// <summary>分页会话标识。</summary>
    public string SessionId { get; init; } = string.Empty;

    /// <summary>之前返回过的剧集 ID。</summary>
    public IReadOnlyList<string> Seen { get; init; } = [];
}

