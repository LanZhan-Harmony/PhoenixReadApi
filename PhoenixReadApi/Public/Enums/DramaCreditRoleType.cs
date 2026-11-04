namespace PhoenixReadApi;

/// <summary>账号在剧集中的署名关系类型。</summary>
public enum DramaCreditRoleType
{
    /// <summary>接口未提供可识别的关系。</summary>
    Unknown = 0,

    /// <summary>主演。</summary>
    Lead = 1,

    /// <summary>配角。</summary>
    Supporting = 2,

    /// <summary>参演。</summary>
    Participant = 3,

    /// <summary>导演。</summary>
    Director = 4,

    /// <summary>编剧。</summary>
    Writer = 5,

    /// <summary>出品方或制作方。</summary>
    Producer = 6,

    /// <summary>其他剧集署名。</summary>
    Other = 7,
}
