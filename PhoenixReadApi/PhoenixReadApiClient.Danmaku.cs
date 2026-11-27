namespace PhoenixReadApi;

public sealed partial class PhoenixReadApiClient
{
    /// <summary>
    /// 获取短剧分集在指定时间窗口内的弹幕。
    /// </summary>
    /// <param name="dramaId">剧集 ID。</param>
    /// <param name="videoId">分集视频 ID。</param>
    /// <param name="startMs">时间窗口起点（毫秒）。</param>
    /// <param name="durationMs">时间窗口长度（毫秒），不是结束时间。</param>
    /// <param name="cancellationToken">取消请求的令牌。</param>
    /// <returns>
    /// 自动合并、去重并按时间排序后的弹幕页。Total 是服务端报告的数量，Items.Count 是实际返回的记录数，
    /// 两者可能不同。
    /// </returns>
    /// <remarks>
    /// 客户端会根据服务端游标自动请求多个时间段，并对“有统计数量但返回空列表”的响应进行有限重试。
    /// durationMs 必须为正数；例如从视频开头读取十分钟可传 0 和 600000。
    /// </remarks>
    public Task<DanmakuPage> GetDramaEpisodeDanmakuAsync(
        string dramaId,
        string videoId,
        long startMs,
        long durationMs,
        CancellationToken cancellationToken = default
    ) => _danmaku.GetAsync(dramaId, videoId, startMs, durationMs, cancellationToken);

    /// <summary>获取独立短视频或视频合集分集在指定时间窗口内的弹幕。</summary>
    /// <param name="videoId">短视频或合集分集视频 ID，必须是数字字符串。</param>
    /// <param name="startMs">起始时间偏移（毫秒）。</param>
    /// <param name="durationMs">查询窗口时长（毫秒），必须大于 0 且不超过 24 小时。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>按时间排序并清洗后的弹幕分页结果。</returns>
    public Task<DanmakuPage> GetShortVideoDanmakuAsync(
        string videoId,
        long startMs,
        long durationMs,
        CancellationToken cancellationToken = default
    ) => _danmaku.GetShortVideoAsync(videoId, startMs, durationMs, cancellationToken);

    /// <summary>为短剧分集发送一条弹幕。</summary>
    /// <param name="dramaId">剧集 ID。</param>
    /// <param name="videoId">分集视频 ID。</param>
    /// <param name="text">弹幕文本。</param>
    /// <param name="offsetMs">弹幕在视频中的时间偏移（毫秒），默认为 0。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>已发送的弹幕实体。</returns>
    /// <remarks>发送弹幕需要登录；服务端可能限制发送频率。</remarks>
    public Task<DanmakuItem> SendDramaEpisodeDanmakuAsync(
        string dramaId,
        string videoId,
        string text,
        long offsetMs = 0,
        CancellationToken cancellationToken = default
    ) => _comment.SendDanmakuAsync(dramaId, videoId, text, offsetMs, cancellationToken);

    /// <summary>为独立短视频或视频合集分集发送一条弹幕。</summary>
    /// <param name="videoId">短视频或合集分集视频 ID，必须是数字字符串。</param>
    /// <param name="text">弹幕文本。</param>
    /// <param name="offsetMs">弹幕在视频中的时间偏移（毫秒），默认为 0。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>已发送的弹幕实体。</returns>
    /// <remarks>发送弹幕需要登录；服务端还可能限制发送频率。</remarks>
    public Task<DanmakuItem> SendShortVideoDanmakuAsync(
        string videoId,
        string text,
        long offsetMs = 0,
        CancellationToken cancellationToken = default
    ) => _comment.SendShortVideoDanmakuAsync(videoId, text, offsetMs, cancellationToken);

    /// <summary>点赞或取消点赞弹幕。</summary>
    /// <param name="commentId">弹幕 ID。</param>
    /// <param name="videoId">视频 ID。</param>
    /// <param name="active">true 表示点赞，false 表示取消点赞。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    public Task<PhoenixOperationResult> SetDanmakuLikeAsync(
        string commentId,
        string videoId,
        bool active = true,
        CancellationToken cancellationToken = default
    ) =>
        _comment.LikeCommentAsync(
            commentId,
            videoId,
            active,
            cancellationToken,
            objectType: 2,
            commentType: 20
        );

    /// <summary>删除一条弹幕。</summary>
    /// <param name="commentId">弹幕 ID。</param>
    /// <param name="videoId">视频 ID。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>服务端操作结果。</returns>
    public Task<PhoenixOperationResult> DeleteDanmakuAsync(
        string commentId,
        string videoId,
        CancellationToken cancellationToken = default
    ) =>
        _comment.DeleteCommentAsync(
            commentId,
            videoId,
            cancellationToken,
            dataType: 4,
            serviceId: 20,
            groupType: 30
        );
}
