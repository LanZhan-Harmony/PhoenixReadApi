using System.Text.Json;
using System.Text.Json.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Library;

/// <summary>书架字段接口返回的动态键值。</summary>
[JsonConverter(typeof(AppBookPackFieldsDataConverter))]
internal sealed class AppBookPackFieldsData
{
    public IReadOnlyDictionary<string, string> Values { get; init; } =
        new Dictionary<string, string>(StringComparer.Ordinal);
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
