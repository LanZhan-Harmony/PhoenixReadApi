using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using PhoenixReadApi.Internal.Contracts.App;
using PhoenixReadApi.Internal.Contracts.App.Auth;
using PhoenixReadApi.Internal.Contracts.App.Catalog;
using PhoenixReadApi.Internal.Contracts.App.Comment;
using PhoenixReadApi.Internal.Contracts.App.Danmaku;
using PhoenixReadApi.Internal.Contracts.App.Details;
using PhoenixReadApi.Internal.Contracts.App.Library;
using PhoenixReadApi.Internal.Contracts.App.Playback;
using PhoenixReadApi.Internal.Contracts.Shared.Drama;
using PhoenixReadApi.Internal.Contracts.Web.Catalog;
using PhoenixReadApi.Internal.Contracts.Web.Details;
using PhoenixReadApi.Internal.Contracts.Web.Playback;
using PhoenixReadApi.Internal.Contracts.Web.Search;

namespace PhoenixReadApi.Internal.Serialization;

[JsonSourceGenerationOptions(
    GenerationMode = JsonSourceGenerationMode.Metadata,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = false
)]
[JsonSerializable(typeof(AppCatalogPayload))]
[JsonSerializable(typeof(AppDetailPayload))]
[JsonSerializable(typeof(AppCommentPayload))]
[JsonSerializable(typeof(AppCommentListPayload))]
[JsonSerializable(typeof(AppUserProfilePayload))]
[JsonSerializable(typeof(AppMediaPayload))]
[JsonSerializable(typeof(WebSuggestionResponseDto))]
[JsonSerializable(typeof(AppResponse<AppCatalogData>))]
[JsonSerializable(typeof(AppResponse<AppDetailsData>))]
[JsonSerializable(typeof(AppResponse<AppPlaybackData>))]
[JsonSerializable(typeof(AppResponse<AppDanmakuData>))]
[JsonSerializable(typeof(AppResponse<AppCommentListData>))]
[JsonSerializable(typeof(AppResponse<AppUserProfileData>))]
[JsonSerializable(typeof(AppResponse<AppUserShelfData>))]
[JsonSerializable(typeof(AppResponse<AppUserActionData>))]
[JsonSerializable(typeof(AppProgressForPayload))]
[JsonSerializable(typeof(AppBookPackFieldsPayload))]
[JsonSerializable(typeof(AppProgressUpdatePayload))]
[JsonSerializable(typeof(AppShelfUpdatePayload))]
[JsonSerializable(typeof(AppFollowActionPayload))]
[JsonSerializable(typeof(AppArticleActionPayload))]
[JsonSerializable(typeof(AppCommentActionPayload))]
[JsonSerializable(typeof(AppCommentAddPayload))]
[JsonSerializable(typeof(AppCommentDeletePayload))]
[JsonSerializable(typeof(AppBusinessParameter))]
[JsonSerializable(typeof(AppReplyListPayload))]
[JsonSerializable(typeof(AppCategoryPayload))]
[JsonSerializable(typeof(AppResponse<AppHistoryData>))]
[JsonSerializable(typeof(AppResponse<AppReadProgressEnvelope>))]
[JsonSerializable(typeof(AppResponse<AppShelfListData>))]
[JsonSerializable(typeof(AppResponse<AppFollowData>))]
[JsonSerializable(typeof(AppResponse<AppReservationData>))]
[JsonSerializable(typeof(AppResponse<AppShareData>))]
[JsonSerializable(typeof(AppResponse<AppLoginData>))]
[JsonSerializable(typeof(AppSessionSnapshot))]
[JsonSerializable(typeof(AppResponse<AppCellChangeData>))]
[JsonSerializable(typeof(AppResponse<AppFeedTabData>))]
[JsonSerializable(typeof(AppResponse<AppReplyListData>))]
[JsonSerializable(typeof(AppResponse<JsonElement>))]
[JsonSerializable(typeof(DramaDto))]
[JsonSerializable(typeof(DramaEpisodeDto))]
[JsonSerializable(typeof(AppPlaybackModel))]
[JsonSerializable(typeof(AppMediaVariantDto))]
[JsonSerializable(typeof(AppVideoMetaDto))]
[JsonSerializable(typeof(AppEncryptionDto))]
[JsonSerializable(typeof(WebCategoryPageDto))]
[JsonSerializable(typeof(WebDetailsPageDto))]
[JsonSerializable(typeof(WebPlayerPageDto))]
[JsonSerializable(typeof(WebSearchPageDto))]
internal partial class SourceGenerationContext : JsonSerializerContext { }

internal static class JsonAotSerializer
{
    /// <summary>
    /// 将对象序列化为 JSON 字符串。
    /// </summary>
    /// <param name="value">要序列化的对象</param>
    /// <returns>序列化后的 JSON 字符串</returns>
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
    /// 将 JSON 字符串反序列化为指定类型。
    /// </summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <param name="value">JSON 字符串</param>
    /// <returns>反序列化后的对象</returns>
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
