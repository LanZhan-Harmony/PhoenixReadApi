using System.Text.Json.Serialization;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Comment;

internal sealed class AppActorProfileData
{
    [JsonPropertyName("data_list")]
    public IReadOnlyList<AppActorWorkRecord> Items { get; init; } = [];
}

internal sealed class AppActorWorkRecord
{
    [JsonPropertyName("data_type")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int DataType { get; init; }

    [JsonPropertyName("video")]
    public AppActorVideo Video { get; init; } = new();
}

internal sealed class AppActorVideo
{
    [JsonPropertyName("video_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("common")]
    public AppActorVideoCommon Common { get; init; } = new();

    [JsonPropertyName("expand")]
    public AppActorVideoExpand Expand { get; init; } = new();
}

internal sealed class AppActorVideoCommon
{
    [JsonPropertyName("title")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("intro")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Introduction { get; init; } = string.Empty;

    [JsonPropertyName("cover")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string CoverUrl { get; init; } = string.Empty;
}

internal sealed class AppActorVideoExpand
{
    [JsonPropertyName("episode_cnt")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int EpisodeCount { get; init; }
}
