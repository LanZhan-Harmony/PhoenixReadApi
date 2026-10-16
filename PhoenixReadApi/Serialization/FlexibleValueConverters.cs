using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using PhoenixReadApi.Internal.Contracts.App;
using PhoenixReadApi.Internal.Contracts.App.Catalog;
using PhoenixReadApi.Internal.Contracts.Shared.Drama;
using PhoenixReadApi.Internal.Contracts.App.Playback;

namespace PhoenixReadApi.Internal.Serialization;

/// <summary>转换红果接口可能以字符串或 JSON 标量值返回的数据。</summary>
internal sealed class FlexibleStringConverter : JsonConverter<string>
{
    public override string Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return string.Empty;
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            return reader.GetString() ?? string.Empty;
        }

        using JsonDocument document = JsonDocument.ParseValue(ref reader);
        return document.RootElement.ToString();
    }

    public override void Write(
        Utf8JsonWriter writer,
        string value,
        JsonSerializerOptions options
    ) => writer.WriteStringValue(value ?? string.Empty);
}

/// <summary>转换可能以 JSON 数字或字符串形式返回的整数。</summary>
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

    private static string ReadText(ref Utf8JsonReader reader)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            return reader.GetString() ?? string.Empty;
        }

        using JsonDocument document = JsonDocument.ParseValue(ref reader);
        return document.RootElement.ToString();
    }
}

/// <summary>转换可能以 JSON 数字或字符串形式返回的 Int64 值。</summary>
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

    private static string ReadText(ref Utf8JsonReader reader)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            return reader.GetString() ?? string.Empty;
        }

        using JsonDocument document = JsonDocument.ParseValue(ref reader);
        return document.RootElement.ToString();
    }
}

/// <summary>转换可能以字符串形式返回的浮点值。</summary>
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

    private static string ReadText(ref Utf8JsonReader reader)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            return reader.GetString() ?? string.Empty;
        }

        using JsonDocument document = JsonDocument.ParseValue(ref reader);
        return document.RootElement.ToString();
    }
}

/// <summary>读取视频模型，兼容对象和 JSON 文本两种返回格式。</summary>
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

/// <summary>读取以数组或键值对象表示的视频版本集合。</summary>
internal sealed class VideoVariantListConverter : JsonConverter<List<JsonElement>>
{
    public override bool HandleNull => true;

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

/// <summary>读取 App 分集记录，兼容数组和键值对象两种 video_list 格式。</summary>
internal sealed class FlexibleVideoListConverter : JsonConverter<IReadOnlyList<AppVideoDto>>
{
    public override bool HandleNull => true;

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

/// <summary>读取剧库的 video_data，兼容数组和键值对象两种格式。</summary>
internal sealed class FlexibleDramaListConverter : JsonConverter<IReadOnlyList<DramaDto>>
{
    public override bool HandleNull => true;

    public override IReadOnlyList<DramaDto> Read(
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
        List<DramaDto> result = [];
        foreach (JsonElement value in values)
        {
            DramaDto? item = JsonAotSerializer.Deserialize<DramaDto>(value.GetRawText());
            if (item is not null)
            {
                result.Add(item);
            }
        }
        return result;
    }

    public override void Write(
        Utf8JsonWriter writer,
        IReadOnlyList<DramaDto> value,
        JsonSerializerOptions options
    )
    {
        writer.WriteStartArray();
        foreach (DramaDto item in value)
        {
            JsonAotSerializer.Serialize(writer, item);
        }

        writer.WriteEndArray();
    }
}

/// <summary>将红果接口返回的字符串或字符串数组统一转换为列表。</summary>
internal sealed class FlexibleStringListConverter : JsonConverter<IReadOnlyList<string>>
{
    public override bool HandleNull => true;

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

/// <summary>读取以数组或键值对象表示的媒体版本。</summary>
internal sealed class FlexibleMediaVariantListConverter
    : JsonConverter<IReadOnlyList<AppMediaVariantDto>>
{
    public override bool HandleNull => true;

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

/// <summary>读取分类结构值，兼容其中嵌套 JSON 文本的情况。</summary>
internal sealed class CategorySchemaConverter : JsonConverter<IReadOnlyList<string>>
{
    public override bool HandleNull => true;

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
