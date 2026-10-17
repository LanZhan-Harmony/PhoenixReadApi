namespace PhoenixReadApi;

/// <summary>一条搜索建议。</summary>
public sealed class SearchSuggestion
{
    /// <summary>建议文本。</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>建议类型。</summary>
    public SearchSuggestionType Type { get; init; } = SearchSuggestionType.Unknown;
}

