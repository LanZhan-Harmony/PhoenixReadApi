namespace PhoenixReadApi;

/// <summary>用户主页展示的内容 Tab 类型。</summary>
public enum UserProfileTabType
{
    /// <summary>未知或未识别的 Tab 类型。</summary>
    Unknown = 0,

    /// <summary>红果圈（服务端类型 7）。</summary>
    Circle = 7,

    /// <summary>点赞内容（服务端类型 9）。</summary>
    Like = 9,

    /// <summary>收藏内容（服务端类型 10）。</summary>
    Favorite = 10,

    /// <summary>短剧作品（服务端类型 23，兼容历史类型 16 或 8）。</summary>
    Work = 23,
}
