namespace PhoenixReadApi;

/// <summary>通知消息的业务类型。</summary>
public enum NoticeType
{
    /// <summary>未知或未指定类型。</summary>
    Unknown = 0,

    /// <summary>追更短剧更新通知。</summary>
    FollowUpdate = 1,

    /// <summary>预约短剧上线通知。</summary>
    ReservationOnline = 2,

    /// <summary>系统与活动通知。</summary>
    System = 3,
}
