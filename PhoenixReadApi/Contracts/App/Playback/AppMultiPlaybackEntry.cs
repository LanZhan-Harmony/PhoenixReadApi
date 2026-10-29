using System.Text.Json.Serialization;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Playback;

/// <summary>多视频播放模型中的单分集项。</summary>
internal sealed class AppMultiPlaybackEntry
{
    [JsonPropertyName("disable_vsr")]
    public bool DisableVsr { get; init; }

    [JsonPropertyName("expire_time")]
    public long ExpireTime { get; init; }

    [JsonPropertyName("video_height")]
    public int VideoHeight { get; init; }

    [JsonPropertyName("video_model")]
    [JsonConverter(typeof(VideoModelDtoConverter))]
    public AppPlaybackModel VideoModel { get; init; } = new();
}
