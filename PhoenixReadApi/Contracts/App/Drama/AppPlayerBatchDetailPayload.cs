using System.Text.Json.Serialization;
using PhoenixReadApi.Internal.Contracts.Shared.Drama;

namespace PhoenixReadApi.Internal.Contracts.App.Drama;

internal sealed class AppPlayerBatchDetailPayload
{
    [JsonPropertyName("series_id")]
    public string SeriesId { get; init; } = string.Empty;

    [JsonPropertyName("dr_scene")]
    public string Scene { get; init; } = "player";

    [JsonPropertyName("biz_param")]
    public AppPlayerBatchBizParam BizParam { get; init; } = new();
}

internal sealed class AppPlayerBatchBizParam
{
    [JsonPropertyName("detail_page_version")]
    public int DetailPageVersion { get; init; }

    [JsonPropertyName("need_all_video_definition")]
    public bool NeedAllVideoDefinition { get; init; }

    [JsonPropertyName("need_mp4_align")]
    public bool NeedMp4Align { get; init; }

    [JsonPropertyName("source")]
    public int Source { get; init; } = 7;

    [JsonPropertyName("use_os_player")]
    public bool UseOsPlayer { get; init; }

    [JsonPropertyName("use_server_dns")]
    public bool UseServerDns { get; init; }
}

internal sealed class AppPlayerSeriesEntry
{
    [JsonPropertyName("video_data")]
    public DramaDto VideoData { get; init; } = new();

    [JsonPropertyName("video_relate_book")]
    public DramaRelateBookContainerDto RelateBook { get; init; } = new();
}
