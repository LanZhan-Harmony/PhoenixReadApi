using System.Text.Json.Serialization;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Interactions;

internal sealed class AppCommonAbResultData
{
    [JsonPropertyName("all_long_press_action_card_playlet")]
    public IReadOnlyList<AppDislikeCardDto> PlayletCards { get; init; } = [];
}

internal sealed class AppDislikeCardDto
{
    [JsonPropertyName("card_type")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int CardType { get; init; }

    [JsonPropertyName("group_list")]
    public IReadOnlyList<AppDislikeGroupDto> Groups { get; init; } = [];
}

internal sealed class AppDislikeGroupDto
{
    [JsonPropertyName("title")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("icon_url")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string IconUrl { get; init; } = string.Empty;

    [JsonPropertyName("selection_list")]
    public IReadOnlyList<AppDislikeSelectionDto> Selections { get; init; } = [];
}

internal sealed class AppDislikeSelectionDto
{
    [JsonPropertyName("selection_id")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int Id { get; init; }

    [JsonPropertyName("selection_text")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Text { get; init; } = string.Empty;

    [JsonPropertyName("selection_head")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Head { get; init; } = string.Empty;

    [JsonPropertyName("selection_nums")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int Nums { get; init; }
}
