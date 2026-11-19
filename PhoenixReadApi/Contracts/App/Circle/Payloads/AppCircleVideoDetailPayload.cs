using System.Text.Json.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Circle;

internal sealed class AppCircleVideoDetailPayload
{
    [JsonPropertyName("series_id")]
    public string SeriesId { get; init; } = string.Empty;

    [JsonPropertyName("biz_param")]
    public AppCircleVideoBizParam BizParam { get; init; } = new();
}

internal sealed class AppCircleVideoBizParam
{
    [JsonPropertyName("detail_page_version")]
    public int DetailPageVersion { get; init; } = 1;

    [JsonPropertyName("disable_digg_stat")]
    public bool DisableDiggStat { get; init; }

    [JsonPropertyName("disable_video_relate_book")]
    public bool DisableVideoRelateBook { get; init; }

    [JsonPropertyName("source")]
    public int Source { get; init; } = 5;

    [JsonPropertyName("video_id_type")]
    public int VideoIdType { get; init; } = 1;
}
