using System.Text.Json.Serialization;
using PhoenixReadApi.Internal.Contracts.Shared.Drama;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Catalog;

internal sealed class AppShortRankResponse
{
    [JsonPropertyName("code")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Code { get; init; } = string.Empty;

    [JsonPropertyName("data")]
    public AppShortRankDataContainer Data { get; init; } = new();
}

internal sealed class AppShortRankDataContainer
{
    [JsonPropertyName("has_more")]
    public bool HasMore { get; init; }

    [JsonPropertyName("next_offset")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int NextOffset { get; init; }

    [JsonPropertyName("session_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string SessionId { get; init; } = string.Empty;

    [JsonPropertyName("cell_view")]
    public AppShortRankCellViewDto CellView { get; init; } = new();
}

internal sealed class AppShortRankCellViewDto
{
    [JsonPropertyName("cell_data")]
    public IReadOnlyList<AppShortRankCellItemDto> CellData { get; init; } = [];
}

internal sealed class AppShortRankCellItemDto
{
    [JsonPropertyName("show_type")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int ShowType { get; init; }

    [JsonPropertyName("cell_name")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string CellName { get; init; } = string.Empty;

    [JsonPropertyName("video_data")]
    public IReadOnlyList<DramaDto> VideoData { get; init; } = [];

    [JsonPropertyName("ugc_user_data")]
    public IReadOnlyList<AppShortRankActorDto> ActorData { get; init; } = [];
}

internal sealed class AppShortRankActorDto
{
    [JsonPropertyName("user_info")]
    public AppShortRankActorUserDto UserInfo { get; init; } = new();

    [JsonPropertyName("rec_text_item")]
    public AppShortRankRecTextDto RecTextItem { get; init; } = new();

    [JsonPropertyName("intro")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Intro { get; init; } = string.Empty;

    [JsonPropertyName("sub_title_list")]
    public IReadOnlyList<AppShortRankSubtitleDto> Subtitles { get; init; } = [];

    [JsonPropertyName("rank_change_data")]
    public AppShortRankChangeDto RankChangeData { get; init; } = new();
}

internal sealed class AppShortRankActorUserDto
{
    [JsonPropertyName("user_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string UserId { get; init; } = string.Empty;

    [JsonPropertyName("base_info")]
    public AppShortRankActorBaseInfoDto BaseInfo { get; init; } = new();
}

internal sealed class AppShortRankActorBaseInfoDto
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

    [JsonPropertyName("actor_id_str")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string ActorIdStr { get; init; } = string.Empty;

    [JsonPropertyName("actor_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string ActorId { get; init; } = string.Empty;

    [JsonIgnore]
    public string EffectiveActorId =>
        !string.IsNullOrWhiteSpace(ActorIdStr) ? ActorIdStr : ActorId;
}

internal sealed class AppShortRankRecTextDto
{
    [JsonPropertyName("RecommendText")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string RecommendText { get; init; } = string.Empty;
}

internal sealed class AppShortRankSubtitleDto
{
    [JsonPropertyName("content")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Content { get; init; } = string.Empty;
}

internal sealed class AppShortRankChangeDto
{
    [JsonPropertyName("rank_change_type")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int ChangeType { get; init; }

    [JsonPropertyName("ChangeNum")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int ChangeNum { get; init; }
}
