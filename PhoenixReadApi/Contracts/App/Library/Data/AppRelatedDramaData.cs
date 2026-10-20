using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Library;

/// <summary>关联剧集接口返回的剧集节点。</summary>
internal sealed class AppRelatedDramaItem
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string CoverUrl { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public int EpisodeCount { get; init; }
    public IReadOnlyList<AppRelatedDramaItem> Children { get; init; } = [];
}

/// <summary>关联剧集接口的规范化响应数据。</summary>
[JsonConverter(typeof(AppRelatedDramaDataConverter))]
internal sealed class AppRelatedDramaData
{
    public IReadOnlyList<AppRelatedDramaItem> Items { get; init; } = [];
}

internal sealed class AppBookPackFieldsDataConverter : JsonConverter<AppBookPackFieldsData>
{
    public override AppBookPackFieldsData Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        using JsonDocument document = JsonDocument.ParseValue(ref reader);
        Dictionary<string, string> values = new(StringComparer.Ordinal);
        if (document.RootElement.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty property in document.RootElement.EnumerateObject())
            {
                values[property.Name] = ValueToString(property.Value);
            }
        }
        return new AppBookPackFieldsData { Values = values };
    }

    public override void Write(
        Utf8JsonWriter writer,
        AppBookPackFieldsData value,
        JsonSerializerOptions options
    )
    {
        writer.WriteStartObject();
        foreach ((string key, string item) in value.Values)
        {
            writer.WriteString(key, item);
        }
        writer.WriteEndObject();
    }

    private static string ValueToString(JsonElement value) =>
        value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : value.GetRawText();
}

internal sealed class AppRelatedDramaDataConverter : JsonConverter<AppRelatedDramaData>
{
    public override AppRelatedDramaData Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        using JsonDocument document = JsonDocument.ParseValue(ref reader);
        List<AppRelatedDramaItem> items = [];
        AddNodes(document.RootElement, items);
        return new AppRelatedDramaData { Items = items };
    }

    public override void Write(
        Utf8JsonWriter writer,
        AppRelatedDramaData value,
        JsonSerializerOptions options
    )
    {
        writer.WriteStartArray();
        foreach (AppRelatedDramaItem item in value.Items)
        {
            WriteItem(writer, item);
        }
        writer.WriteEndArray();
    }

    private static void WriteItem(Utf8JsonWriter writer, AppRelatedDramaItem item)
    {
        writer.WriteStartObject();
        writer.WriteString("series_id", item.Id);
        writer.WriteString("series_title", item.Title);
        writer.WriteString("series_intro", item.Description);
        writer.WriteString("series_cover", item.CoverUrl);
        writer.WriteString("category", item.Category);
        writer.WriteNumber("episode_count", item.EpisodeCount);
        writer.WritePropertyName("children");
        writer.WriteStartArray();
        foreach (AppRelatedDramaItem child in item.Children)
        {
            WriteItem(writer, child);
        }
        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    private static void AddNodes(JsonElement value, List<AppRelatedDramaItem> output)
    {
        if (value.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement item in value.EnumerateArray())
            {
                AddNodes(item, output);
            }
            return;
        }
        if (value.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        string id = ReadString(value, "series_id", "book_id", "id");
        string title = ReadString(value, "series_title", "title", "book_name");
        List<AppRelatedDramaItem> children = [];
        foreach (JsonProperty property in value.EnumerateObject())
        {
            if (property.Value.ValueKind is JsonValueKind.Array or JsonValueKind.Object)
            {
                AddNodes(property.Value, children);
            }
        }

        if (IsNumericId(id) && title.Length > 0)
        {
            output.Add(
                new AppRelatedDramaItem
                {
                    Id = id,
                    Title = title,
                    Description = ReadString(value, "series_intro", "intro", "description"),
                    CoverUrl = ReadString(value, "series_cover", "cover", "cover_url"),
                    Category = ReadString(value, "category", "category_name"),
                    EpisodeCount = ReadInt(value, "episode_cnt", "episode_count"),
                    Children = children,
                }
            );
        }
        else
        {
            output.AddRange(children);
        }
    }

    private static string ReadString(JsonElement value, params string[] names)
    {
        foreach (string name in names)
        {
            if (value.TryGetProperty(name, out JsonElement property))
            {
                return property.ValueKind == JsonValueKind.String
                    ? property.GetString() ?? string.Empty
                    : property.ToString();
            }
        }
        return string.Empty;
    }

    private static int ReadInt(JsonElement value, params string[] names) =>
        int.TryParse(
            ReadString(value, names),
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out int result
        )
            ? result
            : 0;

    private static bool IsNumericId(string value) =>
        value.Length > 0 && value.Length <= 32 && value.All(static c => c is >= '0' and <= '9');
}
