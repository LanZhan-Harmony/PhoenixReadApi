using System.Text.Json.Serialization;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Contracts.Shared.Drama;

internal sealed class DramaCelebrityDto
{
    [JsonPropertyName("celebrity_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("nickname")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("intro")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Introduction { get; init; } = string.Empty;

    [JsonPropertyName("avatar")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string AvatarUrl { get; init; } = string.Empty;

    [JsonPropertyName("role_name")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string RoleName { get; init; } = string.Empty;

    [JsonPropertyName("role_type")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int RoleType { get; init; }

    [JsonPropertyName("sub_title")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string SubTitle { get; init; } = string.Empty;

    [JsonPropertyName("schema")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Schema { get; init; } = string.Empty;
}

/// <summary>详情接口中的剧集署名账号。</summary>
internal sealed class DramaCreatorUserDto
{
    [JsonPropertyName("user_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string UserId { get; init; } = string.Empty;

    [JsonPropertyName("base_info")]
    public DramaCreatorBaseInfoDto BaseInfo { get; init; } = new();

    [JsonPropertyName("user_tag")]
    public DramaCreatorTagDto UserTag { get; init; } = new();
}

internal sealed class DramaCreatorBaseInfoDto
{
    [JsonPropertyName("user_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string UserId { get; init; } = string.Empty;

    [JsonPropertyName("user_name")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("description")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Description { get; init; } = string.Empty;

    [JsonPropertyName("user_avatar")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string AvatarUrl { get; init; } = string.Empty;
}

internal sealed class DramaCreatorTagDto
{
    [JsonPropertyName("sub_title_list")]
    public IReadOnlyList<DramaCreatorSubtitleDto> Subtitles { get; init; } = [];
}

internal sealed class DramaCreatorSubtitleDto
{
    [JsonPropertyName("text")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Text { get; init; } = string.Empty;
}
