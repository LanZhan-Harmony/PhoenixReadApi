using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using PhoenixReadApi.Contracts.App.Catalog;
using PhoenixReadApi.Contracts.App.Playback;
using PhoenixReadApi.Internal.Contracts.App;

namespace PhoenixReadApi.Internal.Serialization;

/// <summary>Converts provider values that may be represented as strings or JSON scalars.</summary>
internal sealed class FlexibleStringConverter : JsonConverter<string?>
{
    public override string? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            return reader.GetString();
        }

        using JsonDocument document = JsonDocument.ParseValue(ref reader);
        return document.RootElement.ToString();
    }

    public override void Write(
        Utf8JsonWriter writer,
        string? value,
        JsonSerializerOptions options
    ) => writer.WriteStringValue(value);
}

/// <summary>Converts an integer that may be returned as a JSON number or string.</summary>
internal sealed class FlexibleIntConverter : JsonConverter<int>
{
    public override int Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var number))
        {
            return number;
        }

        var text = ReadText(ref reader);
        return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out number)
            ? number
            : 0;
    }

    public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options) =>
        writer.WriteNumberValue(value);

    private static string? ReadText(ref Utf8JsonReader reader)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            return reader.GetString();
        }

        using JsonDocument document = JsonDocument.ParseValue(ref reader);
        return document.RootElement.ToString();
    }
}

/// <summary>Converts a long that may be returned as a JSON number or string.</summary>
internal sealed class FlexibleLongConverter : JsonConverter<long>
{
    public override long Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt64(out var number))
        {
            return number;
        }

        var text = ReadText(ref reader);
        return long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out number)
            ? number
            : 0;
    }

    public override void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options) =>
        writer.WriteNumberValue(value);

    private static string? ReadText(ref Utf8JsonReader reader)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            return reader.GetString();
        }

        using JsonDocument document = JsonDocument.ParseValue(ref reader);
        return document.RootElement.ToString();
    }
}

/// <summary>Converts a floating point value that may be returned as a string.</summary>
internal sealed class FlexibleDoubleConverter : JsonConverter<double>
{
    public override double Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        if (reader.TokenType == JsonTokenType.Number && reader.TryGetDouble(out var number))
        {
            return number;
        }

        var text = ReadText(ref reader);
        return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out number)
            ? number
            : 0;
    }

    public override void Write(
        Utf8JsonWriter writer,
        double value,
        JsonSerializerOptions options
    ) => writer.WriteNumberValue(value);

    private static string? ReadText(ref Utf8JsonReader reader)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            return reader.GetString();
        }

        using JsonDocument document = JsonDocument.ParseValue(ref reader);
        return document.RootElement.ToString();
    }
}

/// <summary>Reads the video model whether the provider sends an object or JSON text.</summary>
internal sealed class VideoModelConverter : JsonConverter<JsonElement>
{
    public override JsonElement Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var text = reader.GetString();
            if (string.IsNullOrWhiteSpace(text))
            {
                return default;
            }

            try
            {
                using JsonDocument document = JsonDocument.Parse(text);
                return document.RootElement.Clone();
            }
            catch (JsonException exception)
            {
                throw new JsonException("video_model is not valid JSON.", exception);
            }
        }

        using JsonDocument value = JsonDocument.ParseValue(ref reader);
        return value.RootElement.Clone();
    }

    public override void Write(
        Utf8JsonWriter writer,
        JsonElement value,
        JsonSerializerOptions options
    ) => value.WriteTo(writer);
}

/// <summary>Reads video_model as an object or a JSON string into its App DTO.</summary>
internal sealed class VideoModelDtoConverter : JsonConverter<AppPlaybackModel>
{
    public override AppPlaybackModel? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var text = reader.GetString();
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            try
            {
                return JsonAotSerializer.Deserialize<AppPlaybackModel>(text);
            }
            catch (JsonException exception)
            {
                throw new JsonException("video_model is not valid JSON.", exception);
            }
        }

        using JsonDocument document = JsonDocument.ParseValue(ref reader);
        return JsonAotSerializer.Deserialize<AppPlaybackModel>(document.RootElement.GetRawText());
    }

    public override void Write(
        Utf8JsonWriter writer,
        AppPlaybackModel value,
        JsonSerializerOptions options
    ) => JsonAotSerializer.Serialize(writer, value);
}

/// <summary>Reads a video variant collection represented as an array or keyed object.</summary>
internal sealed class VideoVariantListConverter : JsonConverter<List<JsonElement>>
{
    public override List<JsonElement> Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        using JsonDocument document = JsonDocument.ParseValue(ref reader);
        JsonElement root = document.RootElement;
        if (root.ValueKind == JsonValueKind.Array)
        {
            return [.. root.EnumerateArray().Select(static item => item.Clone())];
        }

        if (root.ValueKind == JsonValueKind.Object)
        {
            return [.. root.EnumerateObject().Select(static item => item.Value.Clone())];
        }

        return [];
    }

    public override void Write(
        Utf8JsonWriter writer,
        List<JsonElement> value,
        JsonSerializerOptions options
    )
    {
        writer.WriteStartArray();
        foreach (JsonElement item in value)
        {
            item.WriteTo(writer);
        }
        writer.WriteEndArray();
    }
}

/// <summary>Reads App episode records whether video_list is an array or keyed object.</summary>
internal sealed class FlexibleVideoListConverter : JsonConverter<IReadOnlyList<AppVideoDto>>
{
    public override IReadOnlyList<AppVideoDto> Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        using JsonDocument document = JsonDocument.ParseValue(ref reader);
        JsonElement root = document.RootElement;
        IEnumerable<JsonElement> values = root.ValueKind switch
        {
            JsonValueKind.Array => root.EnumerateArray(),
            JsonValueKind.Object => root.EnumerateObject().Select(static item => item.Value),
            _ => [],
        };
        List<AppVideoDto> result = [];
        foreach (JsonElement value in values)
        {
            AppVideoDto? item = JsonAotSerializer.Deserialize<AppVideoDto>(value.GetRawText());
            if (item is not null)
            {
                result.Add(item);
            }
        }
        return result;
    }

    public override void Write(
        Utf8JsonWriter writer,
        IReadOnlyList<AppVideoDto> value,
        JsonSerializerOptions options
    )
    {
        writer.WriteStartArray();
        foreach (AppVideoDto item in value)
        {
            JsonAotSerializer.Serialize(writer, item);
        }
        writer.WriteEndArray();
    }
}

/// <summary>Reads catalog video_data whether it is an array or keyed object.</summary>
internal sealed class FlexibleDramaListConverter : JsonConverter<IReadOnlyList<AppDramaDto>>
{
    public override IReadOnlyList<AppDramaDto> Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        using JsonDocument document = JsonDocument.ParseValue(ref reader);
        JsonElement root = document.RootElement;
        IEnumerable<JsonElement> values = root.ValueKind switch
        {
            JsonValueKind.Array => root.EnumerateArray(),
            JsonValueKind.Object => root.EnumerateObject().Select(static item => item.Value),
            _ => [],
        };
        List<AppDramaDto> result = [];
        foreach (JsonElement value in values)
        {
            AppDramaDto? item = JsonAotSerializer.Deserialize<AppDramaDto>(value.GetRawText());
            if (item is not null)
            {
                result.Add(item);
            }
        }
        return result;
    }

    public override void Write(
        Utf8JsonWriter writer,
        IReadOnlyList<AppDramaDto> value,
        JsonSerializerOptions options
    )
    {
        writer.WriteStartArray();
        foreach (AppDramaDto item in value)
        {
            JsonAotSerializer.Serialize(writer, item);
        }

        writer.WriteEndArray();
    }
}

/// <summary>Reads a provider string or string array as a stable list.</summary>
internal sealed class FlexibleStringListConverter : JsonConverter<IReadOnlyList<string>>
{
    public override IReadOnlyList<string> Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        using JsonDocument document = JsonDocument.ParseValue(ref reader);
        JsonElement root = document.RootElement;
        if (root.ValueKind == JsonValueKind.String)
        {
            return [root.GetString() ?? string.Empty];
        }

        if (root.ValueKind == JsonValueKind.Number)
        {
            return [root.GetRawText()];
        }

        if (root.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return
        [
            .. root.EnumerateArray()
                .Where(static item =>
                    item.ValueKind
                        is JsonValueKind.String
                            or JsonValueKind.Number
                            or JsonValueKind.True
                            or JsonValueKind.False
                )
                .Select(static item =>
                    item.ValueKind == JsonValueKind.String
                        ? item.GetString() ?? string.Empty
                        : item.GetRawText()
                ),
        ];
    }

    public override void Write(
        Utf8JsonWriter writer,
        IReadOnlyList<string> value,
        JsonSerializerOptions options
    )
    {
        writer.WriteStartArray();
        foreach (var item in value)
        {
            writer.WriteStringValue(item);
        }

        writer.WriteEndArray();
    }
}

/// <summary>Reads media variants represented as an array or keyed object.</summary>
internal sealed class FlexibleMediaVariantListConverter
    : JsonConverter<IReadOnlyList<AppMediaVariantDto>>
{
    public override IReadOnlyList<AppMediaVariantDto> Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        using JsonDocument document = JsonDocument.ParseValue(ref reader);
        JsonElement root = document.RootElement;
        IEnumerable<JsonElement> values = root.ValueKind switch
        {
            JsonValueKind.Array => root.EnumerateArray(),
            JsonValueKind.Object => root.EnumerateObject().Select(static item => item.Value),
            _ => [],
        };
        List<AppMediaVariantDto> result = [];
        foreach (JsonElement value in values)
        {
            AppMediaVariantDto? item = JsonAotSerializer.Deserialize<AppMediaVariantDto>(
                value.GetRawText()
            );
            if (item is not null)
            {
                result.Add(item);
            }
        }
        return result;
    }

    public override void Write(
        Utf8JsonWriter writer,
        IReadOnlyList<AppMediaVariantDto> value,
        JsonSerializerOptions options
    )
    {
        writer.WriteStartArray();
        foreach (AppMediaVariantDto item in value)
        {
            JsonAotSerializer.Serialize(writer, item);
        }

        writer.WriteEndArray();
    }
}

/// <summary>Reads category schema values that are sometimes nested JSON text.</summary>
internal sealed class CategorySchemaConverter : JsonConverter<List<string>>
{
    public override List<string> Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        using JsonDocument document = JsonDocument.ParseValue(ref reader);
        JsonElement root = document.RootElement;
        if (root.ValueKind == JsonValueKind.String)
        {
            var text = root.GetString();
            if (string.IsNullOrWhiteSpace(text))
            {
                return [];
            }

            try
            {
                using JsonDocument nested = JsonDocument.Parse(text);
                return ReadNames(nested.RootElement);
            }
            catch (JsonException)
            {
                return [text.Trim()];
            }
        }

        return ReadNames(root);
    }

    public override void Write(
        Utf8JsonWriter writer,
        List<string> value,
        JsonSerializerOptions options
    )
    {
        writer.WriteStartArray();
        foreach (var item in value)
        {
            writer.WriteStringValue(item);
        }
        writer.WriteEndArray();
    }

    private static List<string> ReadNames(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Array)
        {
            return [.. root.EnumerateArray().SelectMany(ReadNames)];
        }

        if (root.ValueKind == JsonValueKind.Object)
        {
            foreach (var name in new[] { "name", "title", "label", "tag" })
            {
                if (
                    root.TryGetProperty(name, out JsonElement value)
                    && value.ValueKind == JsonValueKind.String
                )
                {
                    return [value.GetString()!.Trim()];
                }
            }
        }

        var text = root.ToString().Trim();
        return text.Length == 0 ? [] : [text];
    }
}
