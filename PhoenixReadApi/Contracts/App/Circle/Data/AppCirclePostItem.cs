using System.Text.Json.Serialization;
using PhoenixReadApi.Internal.Contracts.App.Comment;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Circle;

internal sealed class AppCirclePostItem
{
    [JsonPropertyName("post_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string PostId { get; init; } = string.Empty;

    [JsonPropertyName("common")]
    public AppCirclePostCommon Common { get; init; } = new();

    [JsonPropertyName("stat")]
    public AppCirclePostStat Stat { get; init; } = new();

    [JsonPropertyName("user_action")]
    public AppCirclePostUserAction UserAction { get; init; } = new();

    [JsonPropertyName("expand")]
    public AppCirclePostExpand Expand { get; init; } = new();
}

internal sealed class AppCirclePostCommon
{
    [JsonPropertyName("title")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("create_timestamp")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long CreatedAt { get; init; }

    [JsonPropertyName("cover")]
    public AppCirclePostCover Cover { get; init; } = new();

    [JsonPropertyName("content")]
    public AppCirclePostContent Content { get; init; } = new();

    [JsonPropertyName("user_info")]
    public AppCommentUserInfo UserInfo { get; init; } = new();
}

internal sealed class AppCirclePostCover
{
    [JsonPropertyName("web_url")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Url { get; init; } = string.Empty;

    [JsonPropertyName("width")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int Width { get; init; }

    [JsonPropertyName("height")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int Height { get; init; }
}

internal sealed class AppCirclePostContent
{
    [JsonPropertyName("pure_text")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string PureText { get; init; } = string.Empty;

    [JsonPropertyName("text")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Text { get; init; } = string.Empty;

    [JsonPropertyName("text_exts")]
    public IReadOnlyList<AppCirclePostTextExt> TextExts { get; init; } = [];
}

internal sealed class AppCirclePostTextExt
{
    [JsonPropertyName("text")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Text { get; init; } = string.Empty;

    [JsonPropertyName("text_ext")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int TextExt { get; init; }
}

internal sealed class AppCirclePostStat
{
    [JsonPropertyName("digg_count")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long DiggCount { get; init; }

    [JsonPropertyName("reply_count")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long ReplyCount { get; init; }

    [JsonPropertyName("collect_count")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long CollectCount { get; init; }

    [JsonPropertyName("forwarded_count")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long ForwardedCount { get; init; }
}

internal sealed class AppCirclePostUserAction
{
    [JsonPropertyName("user_digg")]
    public bool UserDigg { get; init; }

    [JsonPropertyName("user_collect")]
    public bool UserCollect { get; init; }
}

internal sealed class AppCirclePostExpand
{
    [JsonPropertyName("videos")]
    public IReadOnlyList<AppCircleRelatedDrama> Videos { get; init; } = [];
}

internal sealed class AppCircleRelatedDrama
{
    [JsonPropertyName("series_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string SeriesId { get; init; } = string.Empty;

    [JsonPropertyName("title")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("cover")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string CoverUrl { get; init; } = string.Empty;

    [JsonPropertyName("intro")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Introduction { get; init; } = string.Empty;

    [JsonPropertyName("episode_cnt")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int EpisodeCount { get; init; }

    [JsonPropertyName("play_cnt")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long PlayCount { get; init; }
}

/// <summary>用于从 PostContent.Text 中反序列化材质数据（图集、话题等）。</summary>
internal sealed class AppCirclePostMaterialContainer
{
    [JsonPropertyName("materials")]
    public IReadOnlyList<AppCirclePostMaterial> Materials { get; init; } = [];
}

internal sealed class AppCirclePostMaterial
{
    [JsonPropertyName("type")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Type { get; init; } = string.Empty;

    [JsonPropertyName("data")]
    public AppCirclePostMaterialData Data { get; init; } = new();
}

internal sealed class AppCirclePostMaterialData
{
    [JsonPropertyName("datas")]
    public IReadOnlyList<AppCirclePostImageItem> Images { get; init; } = [];

    [JsonPropertyName("topic_tag")]
    public AppCirclePostTopicTag? TopicTag { get; init; }
}

internal sealed class AppCirclePostImageItem
{
    [JsonPropertyName("web_url")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string WebUrl { get; init; } = string.Empty;

    [JsonPropertyName("width")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int Width { get; init; }

    [JsonPropertyName("height")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int Height { get; init; }
}

internal sealed class AppCirclePostTopicTag
{
    [JsonPropertyName("tag")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Tag { get; init; } = string.Empty;

    [JsonPropertyName("tag_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string TagId { get; init; } = string.Empty;

    [JsonPropertyName("schema")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Schema { get; init; } = string.Empty;
}
