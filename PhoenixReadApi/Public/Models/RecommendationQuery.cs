namespace PhoenixReadApi;

/// <summary>App 推荐接口的输入游标。</summary>
public class RecommendationQuery
{
    /// <summary>推荐题材。</summary>
    public string Genre { get; init; } = string.Empty;

    /// <summary>红果接口分页偏移量。</summary>
    public int Offset { get; init; }

    /// <summary>红果接口分页会话标识。</summary>
    public string SessionId { get; init; } = string.Empty;

    /// <summary>之前返回过的红果剧集 ID。</summary>
    public IReadOnlyList<string> Seen { get; init; } = [];
}

