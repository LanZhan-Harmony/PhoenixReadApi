using System.Text.Json.Serialization;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Contracts.Shared.Drama;

internal sealed class DramaHotScoreDto
{
    [JsonPropertyName("score")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Score { get; init; } = string.Empty;

    [JsonPropertyName("text")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Text { get; init; } = string.Empty;
}

internal sealed class DramaCategoryDto
{
    [JsonPropertyName("category_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Name { get; init; } = string.Empty;
}

internal sealed class DramaTagDto
{
    public string Name { get; init; } = string.Empty;
}

internal sealed class DramaSubtitleDto
{
    [JsonPropertyName("content")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Content { get; init; } = string.Empty;
}

internal sealed class DramaPayInfoDto
{
    [JsonPropertyName("pay_type")]
    public int PayType { get; init; }
}
