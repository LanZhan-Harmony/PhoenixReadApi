namespace PhoenixReadApi;

/// <summary>用户主页作品（短剧、视频合集、独立短视频、图文帖子或剧评的联合类型）。</summary>
public readonly union UserWork(Drama, VideoAlbum, ShortVideo, Post, UserReview);
