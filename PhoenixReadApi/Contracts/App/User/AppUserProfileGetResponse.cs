using System.Text.Json.Serialization;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.User;

internal sealed class AppUserProfileGetResponse
{
    [JsonPropertyName("ugc_user")]
    public AppUgcUser UgcUser { get; init; } = new();

    [JsonPropertyName("bg_cover")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string BgCover { get; init; } = string.Empty;

    [JsonPropertyName("bg_covers")]
    public IReadOnlyList<string> BgCovers { get; init; } = [];
}

internal sealed class AppUgcUser
{
    [JsonPropertyName("user_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string UserId { get; init; } = string.Empty;

    [JsonPropertyName("base_info")]
    public AppUgcUserBaseInfo BaseInfo { get; init; } = new();

    [JsonPropertyName("user_tag")]
    public AppUgcUserTag UserTag { get; init; } = new();

    [JsonPropertyName("user_relation")]
    public AppUgcUserRelation UserRelation { get; init; } = new();

    [JsonPropertyName("bg_cover")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string BgCover { get; init; } = string.Empty;
}

internal sealed class AppUgcUserBaseInfo
{
    [JsonPropertyName("user_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string UserId { get; init; } = string.Empty;

    [JsonPropertyName("user_name")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string UserName { get; init; } = string.Empty;

    [JsonPropertyName("user_avatar")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string UserAvatar { get; init; } = string.Empty;

    [JsonPropertyName("description")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Description { get; init; } = string.Empty;

    [JsonPropertyName("biz_user_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string BizUserId { get; init; } = string.Empty;

    [JsonPropertyName("actor_id")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long ActorId { get; init; }

    [JsonPropertyName("gender")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int Gender { get; init; }

    [JsonPropertyName("birthday")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Birthday { get; init; } = string.Empty;
}

internal sealed class AppUgcUserTag
{
    [JsonPropertyName("is_blue_vip")]
    public bool IsBlueVip { get; init; }

    [JsonPropertyName("user_title_info")]
    public IReadOnlyList<AppUgcUserTitleInfo> UserTitleInfo { get; init; } = [];

    [JsonPropertyName("sub_title_list")]
    public IReadOnlyList<AppUgcSubTitleItem> SubTitleList { get; init; } = [];

    [JsonPropertyName("honor_tag_brief_info")]
    public AppUgcHonorTagBriefInfo HonorTagBriefInfo { get; init; } = new();

    [JsonPropertyName("creator_types_full")]
    public IReadOnlyList<int> CreatorTypesFull { get; init; } = [];
}

internal sealed class AppUgcUserTitleInfo
{
    [JsonPropertyName("zh_title")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string ZhTitle { get; init; } = string.Empty;

    [JsonPropertyName("en_title")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string EnTitle { get; init; } = string.Empty;
}

internal sealed class AppUgcSubTitleItem
{
    [JsonPropertyName("text")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Text { get; init; } = string.Empty;
}

internal sealed class AppUgcHonorTagBriefInfo
{
    [JsonPropertyName("honor_tag_detail_list")]
    public IReadOnlyList<AppUgcHonorTagDetail> HonorTagDetailList { get; init; } = [];
}

internal sealed class AppUgcHonorTagDetail
{
    [JsonPropertyName("name")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("small_img_url")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string SmallImgUrl { get; init; } = string.Empty;

    [JsonPropertyName("img_url")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string ImgUrl { get; init; } = string.Empty;
}

internal sealed class AppUgcUserRelation
{
    [JsonPropertyName("can_follow")]
    public bool CanFollow { get; init; }

    [JsonPropertyName("relation_type")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int RelationType { get; init; }

    [JsonPropertyName("fans_num")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long FansNum { get; init; }

    [JsonPropertyName("follow_user_num")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long FollowUserNum { get; init; }

    [JsonPropertyName("recv_digged_count")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long RecvDiggedCount { get; init; }
}
