using System.Text.Json.Serialization;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Users;

internal sealed class AppUserRelationListData
{
    [JsonPropertyName("common_list_info")]
    public AppUserRelationListInfo ListInfo { get; init; } = new();

    [JsonPropertyName("is_privacy_on")]
    public bool IsPrivacyOn { get; init; }

    [JsonPropertyName("ugc_user_list")]
    public IReadOnlyList<AppUserRelationUgcUser> Users { get; init; } = [];
}

internal sealed class AppUserRelationListInfo
{
    [JsonPropertyName("cursor")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Cursor { get; init; } = string.Empty;

    [JsonPropertyName("has_more")]
    public bool HasMore { get; init; }
}

internal sealed class AppUserRelationUgcUser
{
    [JsonPropertyName("user_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string UserId { get; init; } = string.Empty;

    [JsonPropertyName("base_info")]
    public AppUgcUserBaseInfo BaseInfo { get; init; } = new();

    [JsonPropertyName("user_relation")]
    public AppUgcUserRelation UserRelation { get; init; } = new();

    [JsonPropertyName("user_tag")]
    public AppUgcUserTag UserTag { get; init; } = new();
}
