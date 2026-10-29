using System.Text.Json.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Users;

internal sealed class AppVipInfoDto
{
    [JsonPropertyName("is_vip")]
    public string IsVip { get; init; } = string.Empty;

    [JsonPropertyName("expire_time")]
    public string ExpireTime { get; init; } = string.Empty;
}

internal sealed class AppCurrentUserInfoData
{
    [JsonPropertyName("user_id")]
    public string UserId { get; init; } = string.Empty;

    [JsonPropertyName("biz_user_id")]
    public string BizUserId { get; init; } = string.Empty;

    [JsonPropertyName("user_name")]
    public string UserName { get; init; } = string.Empty;

    [JsonPropertyName("avatar_url")]
    public string AvatarUrl { get; init; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; init; } = string.Empty;

    [JsonPropertyName("gender")]
    public int Gender { get; init; }

    [JsonPropertyName("vip_info")]
    public AppVipInfoDto VipInfo { get; init; } = new();
}
