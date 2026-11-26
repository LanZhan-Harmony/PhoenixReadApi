namespace PhoenixReadApi;

/// <summary>红果圈 Feed 流内容项（图文帖子或独立短视频的联合类型）。</summary>
public readonly union CircleItem(Post, ShortVideo);
