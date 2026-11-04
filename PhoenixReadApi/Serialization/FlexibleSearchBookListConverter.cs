using System.Text.Json;
using System.Text.Json.Serialization;
using PhoenixReadApi.Internal.Contracts.App.Search;

namespace PhoenixReadApi.Internal.Serialization;

/// <summary>读取搜索结果的 book_data，兼容数组和单对象两种格式。</summary>
internal sealed class FlexibleSearchBookListConverter
    : JsonConverter<IReadOnlyList<AppSearchBookDto>>
{
    public override bool HandleNull => true;

    public override IReadOnlyList<AppSearchBookDto> Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        using JsonDocument document = JsonDocument.ParseValue(ref reader);
        JsonElement root = document.RootElement;
        List<AppSearchBookDto> result = [];
        if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement value in root.EnumerateArray())
            {
                AppSearchBookDto? item = JsonAotSerializer.Deserialize<AppSearchBookDto>(
                    value.GetRawText()
                );
                if (item is not null)
                {
                    result.Add(item);
                }
            }
        }
        else if (root.ValueKind == JsonValueKind.Object)
        {
            AppSearchBookDto? item = JsonAotSerializer.Deserialize<AppSearchBookDto>(
                root.GetRawText()
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
        IReadOnlyList<AppSearchBookDto> value,
        JsonSerializerOptions options
    )
    {
        throw new NotSupportedException();
    }
}
