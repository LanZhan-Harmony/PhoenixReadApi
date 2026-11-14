using System.Text.Json.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Drama;

/// <summary>合集详情接口请求载荷。</summary>
internal sealed class AppAlbumPayload
{
    [JsonPropertyName("album_id")]
    public string AlbumId { get; init; } = string.Empty;

    [JsonPropertyName("biz_param")]
    public AppAlbumBizParam BusinessParameter { get; init; } = new();

    [JsonPropertyName("need_video_detail_info")]
    public bool NeedVideoDetailInfo { get; init; } = true;

    [JsonPropertyName("series_ids")]
    public IReadOnlyList<long> SeriesIds { get; init; } = [];
}

internal sealed class AppAlbumBizParam
{
    [JsonPropertyName("video_platform")]
    public int VideoPlatform { get; init; } = 3;
}
