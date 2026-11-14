using System.Text.Json;
using System.Text.Json.Serialization;
using PhoenixReadApi.Internal.Contracts.App.Drama;

namespace PhoenixReadApi.Internal.Serialization;

/// <summary>
/// 兼容红果服务端直接返回 JSON 对象或内嵌 JSON 字符串的 album_data。
/// </summary>
internal sealed class FlexibleAlbumModelConverter : JsonConverter<AppAlbumModel>
{
    public override bool HandleNull => true;

    public override AppAlbumModel Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return new AppAlbumModel();
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            string text = reader.GetString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(text))
            {
                return new AppAlbumModel();
            }

            try
            {
                return JsonAotSerializer.Deserialize<AppAlbumModel>(text) ?? new AppAlbumModel();
            }
            catch (JsonException)
            {
                return new AppAlbumModel();
            }
        }

        using JsonDocument document = JsonDocument.ParseValue(ref reader);
        return JsonAotSerializer.Deserialize<AppAlbumModel>(document.RootElement)
            ?? new AppAlbumModel();
    }

    public override void Write(
        Utf8JsonWriter writer,
        AppAlbumModel value,
        JsonSerializerOptions options
    ) => JsonAotSerializer.Serialize(writer, value);
}
