namespace PhoenixReadApi;

/// <summary>剧集的更新状态。</summary>
public enum DramaReleaseStatus
{
    /// <summary>接口未提供可识别的状态。</summary>
    Unknown = 0,

    /// <summary>正在更新或连载中。</summary>
    Ongoing = 1,

    /// <summary>已完结。</summary>
    Finished = 2,
}
