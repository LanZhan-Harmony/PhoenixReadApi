namespace PhoenixReadApi;

/// <summary>短剧搜索分类标签类型。</summary>
public enum SearchTab
{
    /// <summary>综合短剧（对应服务端的 tab_type 11）。</summary>
    ShortPlay = 11,

    /// <summary>剧集长剧（对应 tab_type 29）。</summary>
    SeriesVideo = 29,

    /// <summary>PUGC 视频（对应 tab_type 30）。</summary>
    PugcVideo = 30,

    /// <summary>互动讨论（对应 tab_type 31）。</summary>
    Discussion = 31,

    /// <summary>原著小说（对应 tab_type 1）。</summary>
    CommonBook = 1,

    /// <summary>红果创作者/用户（对应 tab_type 27）。</summary>
    User = 27,

    /// <summary>有声听书（对应 tab_type 2）。</summary>
    Voice = 2,
}
