namespace PhoenixReadApi;

/// <summary>一条搜索建议。</summary>
public class SearchSuggestion
{
    /// <summary>建议文本。</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>红果接口返回的建议类型。</summary>
    public string Type { get; init; } = string.Empty;
}

