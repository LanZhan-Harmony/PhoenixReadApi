namespace PhoenixReadApi;

/// <summary>搜索建议的来源类型。</summary>
public enum SearchSuggestionType
{
    /// <summary>接口未提供可识别的类型。</summary>
    Unknown = 0,

    /// <summary>短剧名称。</summary>
    ShortPlayName = 1,

    /// <summary>短剧分类。</summary>
    ShortPlayCategory = 2,

    /// <summary>普通搜索词。</summary>
    CommonQuery = 3,

    /// <summary>演员名称。</summary>
    ActorName = 4,

    /// <summary>短剧演员相关词。</summary>
    ShortPlayActor = 5,
}
