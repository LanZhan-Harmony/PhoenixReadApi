using System.Text.Json;
using System.Text.Json.Serialization;
using PhoenixReadApi.Internal.Contracts.App.Comment;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Danmaku;

internal sealed class AppDanmakuData
{
    [JsonPropertyName("data_list")]
    public IReadOnlyList<AppCommentRecord> Items { get; init; } = [];

    [JsonPropertyName("extra")]
    public AppDanmakuExtra Extra { get; init; } = new();

    [JsonPropertyName("common_list_info")]
    public AppDanmakuListInfo ListInfo { get; init; } = new();
}

internal sealed class AppDanmakuListInfo
{
    [JsonPropertyName("cursor")]
    public AppDanmakuCursor Cursor { get; init; } = new();

    [JsonPropertyName("has_more")]
    public bool HasMore { get; init; }

    [JsonPropertyName("total")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long Total { get; init; }
}

[JsonConverter(typeof(AppDanmakuCursorConverter))]
internal sealed class AppDanmakuCursor
{
    [JsonPropertyName("start_offset_time")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long StartOffsetTime { get; init; }

    [JsonPropertyName("end_offset_time")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long EndOffsetTime { get; init; }

    [JsonPropertyName("danmaku_count")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long DanmakuCount { get; init; }
}

internal sealed class AppDanmakuCursorConverter : JsonConverter<AppDanmakuCursor>
{
    public override AppDanmakuCursor Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            string value = reader.GetString() ?? string.Empty;
            if (value.Length == 0)
            {
                return new AppDanmakuCursor();
            }
            try
            {
                using JsonDocument document = JsonDocument.Parse(value);
                return ReadObject(document.RootElement);
            }
            catch (JsonException)
            {
                return new AppDanmakuCursor();
            }
        }
        if (reader.TokenType == JsonTokenType.StartObject)
        {
            using JsonDocument document = JsonDocument.ParseValue(ref reader);
            return ReadObject(document.RootElement);
        }
        if (reader.TokenType == JsonTokenType.Null)
        {
            return new AppDanmakuCursor();
        }
        reader.Skip();
        return new AppDanmakuCursor();
    }

    public override void Write(
        Utf8JsonWriter writer,
        AppDanmakuCursor value,
        JsonSerializerOptions options
    )
    {
        writer.WriteStartObject();
        writer.WriteNumber("start_offset_time", value.StartOffsetTime);
        writer.WriteNumber("end_offset_time", value.EndOffsetTime);
        writer.WriteNumber("danmaku_count", value.DanmakuCount);
        writer.WriteEndObject();
    }

    private static AppDanmakuCursor ReadObject(JsonElement value) =>
        value.ValueKind == JsonValueKind.Object
            ? new AppDanmakuCursor
            {
                StartOffsetTime = ReadLong(value, "start_offset_time"),
                EndOffsetTime = ReadLong(value, "end_offset_time"),
                DanmakuCount = Math.Max(0, ReadLong(value, "danmaku_count")),
            }
            : new AppDanmakuCursor();

    private static long ReadLong(JsonElement value, string name) =>
        value.TryGetProperty(name, out JsonElement item) && item.TryGetInt64(out long result)
            ? result
        : long.TryParse(item.ToString(), out result) ? result
        : 0;
}

internal sealed class AppDanmakuExtra
{
    [JsonPropertyName("next_query_danmaku_list_time")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long NextTime { get; init; }
}
