using System.Text.Json;
using System.Text.Json.Serialization;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Library;

[JsonConverter(typeof(AppReadProgressEnvelopeConverter))]
internal sealed class AppReadProgressEnvelope
{
    [JsonPropertyName("video_data")]
    public IReadOnlyList<AppReadProgressItem> Items { get; init; } = [];

    [JsonPropertyName("local_book_data")]
    public IReadOnlyList<AppReadProgressItem> LocalItems { get; init; } = [];

    [JsonPropertyName("data")]
    public IReadOnlyList<AppReadProgressItem> DataItems { get; init; } = [];
}

/// <summary>兼容播放进度接口的 object、array 和顶层列表三种返回形态。</summary>
internal sealed class AppReadProgressEnvelopeConverter : JsonConverter<AppReadProgressEnvelope>
{
    public override AppReadProgressEnvelope Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        using JsonDocument document = JsonDocument.ParseValue(ref reader);
        JsonElement root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
        {
            // The list endpoint uses data for ordinary reading records. Those
            // records are not short-drama video progress and must not be mapped
            // as ReadProgress items.
            return new AppReadProgressEnvelope();
        }

        return new AppReadProgressEnvelope
        {
            Items = ReadItems(root, "video_data"),
            LocalItems = ReadItems(root, "local_book_data"),
        };
    }

    public override void Write(
        Utf8JsonWriter writer,
        AppReadProgressEnvelope value,
        JsonSerializerOptions options
    )
    {
        writer.WriteStartObject();
        writer.WritePropertyName("video_data");
        WriteItems(writer, value.Items);
        writer.WritePropertyName("local_book_data");
        WriteItems(writer, value.LocalItems);
        writer.WriteEndObject();
    }

    private static IReadOnlyList<AppReadProgressItem> ReadItems(
        JsonElement root,
        string propertyName
    )
    {
        if (!root.TryGetProperty(propertyName, out JsonElement value))
        {
            return [];
        }

        IEnumerable<JsonElement> values = value.ValueKind switch
        {
            JsonValueKind.Array => value.EnumerateArray(),
            JsonValueKind.Object => value.EnumerateObject().Select(static item => item.Value),
            _ => [],
        };
        List<AppReadProgressItem> result = [];
        foreach (JsonElement item in values)
        {
            AppReadProgressItem? progress = JsonAotSerializer.Deserialize<AppReadProgressItem>(
                item.GetRawText()
            );
            if (progress is not null)
            {
                result.Add(progress);
            }
        }
        return result;
    }

    private static void WriteItems(Utf8JsonWriter writer, IReadOnlyList<AppReadProgressItem> items)
    {
        writer.WriteStartArray();
        foreach (AppReadProgressItem item in items)
        {
            JsonAotSerializer.Serialize(writer, item);
        }
        writer.WriteEndArray();
    }
}
