using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using PhoenixReadApi.Contracts.App.Catalog;
using PhoenixReadApi.Contracts.App.Comment;
using PhoenixReadApi.Contracts.App.Danmaku;
using PhoenixReadApi.Contracts.App.Playback;
using PhoenixReadApi.Contracts.Web.Catalog;
using PhoenixReadApi.Contracts.Web.Playback;
using PhoenixReadApi.Contracts.Web.Search;
using PhoenixReadApi.Internal.Contracts.App;
using PhoenixReadApi.Internal.Contracts.App.Search;
using PhoenixReadApi.Internal.Contracts.Web;

namespace PhoenixReadApi.Internal.Serialization;

[JsonSourceGenerationOptions(
    GenerationMode = JsonSourceGenerationMode.Metadata,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = false
)]
[JsonSerializable(typeof(AppCatalogPayload))]
[JsonSerializable(typeof(AppDetailPayload))]
[JsonSerializable(typeof(AppMediaPayload))]
[JsonSerializable(typeof(AppCommentPayload))]
[JsonSerializable(typeof(PlaybackReference))]
[JsonSerializable(typeof(SuggestionData))]
[JsonSerializable(typeof(AppResponse<AppCatalogData>))]
[JsonSerializable(typeof(AppResponse<AppDetailsData>))]
[JsonSerializable(typeof(AppResponse<AppPlaybackData>))]
[JsonSerializable(typeof(AppResponse<AppDanmakuData>))]
[JsonSerializable(typeof(AppDramaDto))]
[JsonSerializable(typeof(AppVideoDto))]
[JsonSerializable(typeof(AppPlaybackModel))]
[JsonSerializable(typeof(AppMediaVariantDto))]
[JsonSerializable(typeof(AppVideoMetaDto))]
[JsonSerializable(typeof(AppEncryptionDto))]
[JsonSerializable(typeof(WebCategoryPageDto))]
[JsonSerializable(typeof(WebDetailsPageDto))]
[JsonSerializable(typeof(WebPlayerPageDto))]
[JsonSerializable(typeof(WebSearchPageDto))]
[JsonSerializable(typeof(BackupPlaybackResponseDto))]
internal partial class SourceGenerationContext : JsonSerializerContext { }

internal static class JsonAotSerializer
{
    /// <summary>
    /// Serializes an object to a JSON string.
    /// </summary>
    /// <param name="value">The object to serialize</param>
    /// <returns>The serialized JSON string</returns>
    internal static string Serialize<T>(T value)
    {
        if (
            SourceGenerationContext.Default.GetTypeInfo(typeof(T))
            is not JsonTypeInfo<T> jsonTypeInfo
        )
        {
            throw new ArgumentNullException(
                nameof(value),
                $"{typeof(T)} is not declared in the SourceGenerationContext attributes."
            );
        }
        return JsonSerializer.Serialize(value, jsonTypeInfo);
    }

    internal static byte[] SerializeToUtf8Bytes<T>(T value)
    {
        if (
            SourceGenerationContext.Default.GetTypeInfo(typeof(T))
            is not JsonTypeInfo<T> jsonTypeInfo
        )
        {
            throw new ArgumentNullException(
                nameof(value),
                $"{typeof(T)} is not declared in the SourceGenerationContext attributes."
            );
        }
        return JsonSerializer.SerializeToUtf8Bytes(value, jsonTypeInfo);
    }

    internal static void Serialize<T>(Utf8JsonWriter writer, T value)
    {
        if (
            SourceGenerationContext.Default.GetTypeInfo(typeof(T))
            is not JsonTypeInfo<T> jsonTypeInfo
        )
        {
            throw new ArgumentNullException(
                nameof(value),
                $"{typeof(T)} is not declared in the SourceGenerationContext attributes."
            );
        }
        JsonSerializer.Serialize(writer, value, jsonTypeInfo);
    }

    /// <summary>
    /// Deserializes a JSON string to the specified type.
    /// </summary>
    /// <typeparam name="T">The target type</typeparam>
    /// <param name="value">The JSON string</param>
    /// <returns>The deserialized object</returns>
    internal static T? Deserialize<T>(string value)
    {
        if (
            SourceGenerationContext.Default.GetTypeInfo(typeof(T))
            is not JsonTypeInfo<T> jsonTypeInfo
        )
        {
            throw new ArgumentNullException(
                nameof(T),
                $"{typeof(T)} is not declared in the SourceGenerationContext attributes."
            );
        }
        return JsonSerializer.Deserialize(value, jsonTypeInfo);
    }

    internal static T? Deserialize<T>(ReadOnlySpan<byte> value)
    {
        if (
            SourceGenerationContext.Default.GetTypeInfo(typeof(T))
            is not JsonTypeInfo<T> jsonTypeInfo
        )
        {
            throw new ArgumentNullException(
                nameof(T),
                $"{typeof(T)} is not declared in the SourceGenerationContext attributes."
            );
        }
        return JsonSerializer.Deserialize(value, jsonTypeInfo);
    }
}
