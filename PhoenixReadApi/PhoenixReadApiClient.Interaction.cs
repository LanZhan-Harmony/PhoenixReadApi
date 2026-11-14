namespace PhoenixReadApi;

public sealed partial class PhoenixReadApiClient
{
    /// <summary>获取不感兴趣与负反馈选项面板配置。</summary>
    /// <param name="cardType">面板业务卡片类型，短剧默认为 2。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>不感兴趣负反馈面板配置信息，包含可选的分组和反馈项。</returns>
    public Task<DislikePanel> GetDislikePanelAsync(
        int cardType = 2,
        CancellationToken cancellationToken = default
    ) => _interactions.GetDislikePanelAsync(cardType, cancellationToken);

    /// <summary>提交对某部短剧或视频的不感兴趣负反馈以调整推荐算法。</summary>
    /// <param name="dramaId">短剧 ID，必须是由数字构成的字符串。</param>
    /// <param name="videoId">视频 ID；可为空字符串。</param>
    /// <param name="recommendInfo">推荐来源信息串；可为空字符串。</param>
    /// <param name="option">从面板中选中的负反馈选项。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>操作是否成功。</returns>
    public Task<bool> SubmitDislikeAsync(
        string dramaId,
        string videoId,
        string recommendInfo,
        DislikeOption option,
        CancellationToken cancellationToken = default
    ) => _interactions.SubmitDislikeAsync(dramaId, videoId, recommendInfo, option, cancellationToken);
}
