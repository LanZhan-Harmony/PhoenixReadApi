using System.Text.Json;
using System.Text.Json.Serialization;
using PhoenixReadApi.Internal.Contracts.App.Library;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App;

/// <summary>红果 App 接口返回的通用响应封装。</summary>
internal sealed class AppResponse<T>
{
    [JsonPropertyName("code")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Code { get; init; } = string.Empty;

    [JsonPropertyName("data")]
    public T Data { get; init; } = default!;

    [JsonPropertyName("message")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Message { get; init; } = string.Empty;

    [JsonPropertyName("BaseResp")]
    public AppBaseResponse BaseResponse { get; init; } = new();
}

/// <summary>播放进度接口的响应。该接口把两个列表放在通用 data 字段之外。</summary>
internal sealed class AppReadProgressResponse
{
    [JsonPropertyName("code")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Code { get; init; } = string.Empty;

    [JsonPropertyName("message")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Message { get; init; } = string.Empty;

    [JsonPropertyName("BaseResp")]
    public AppBaseResponse BaseResponse { get; init; } = new();

    [JsonPropertyName("video_data")]
    public IReadOnlyList<AppReadProgressItem> VideoData { get; init; } = [];

    [JsonPropertyName("local_book_data")]
    public IReadOnlyList<AppReadProgressItem> LocalBookData { get; init; } = [];

    [JsonPropertyName("data")]
    public AppReadProgressEnvelope Data { get; init; } = new();
}

/// <summary>成功但没有业务返回字段的写操作响应。</summary>
[JsonConverter(typeof(AppOperationResponseConverter))]
internal sealed class AppOperationResponse { }

internal sealed class AppOperationResponseConverter : JsonConverter<AppOperationResponse>
{
    public override AppOperationResponse Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        reader.Skip();
        return new AppOperationResponse();
    }

    public override void Write(
        Utf8JsonWriter writer,
        AppOperationResponse value,
        JsonSerializerOptions options
    )
    {
        writer.WriteStartObject();
        writer.WriteEndObject();
    }
}

internal sealed class AppBaseResponse
{
    [JsonPropertyName("StatusCode")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string StatusCode { get; init; } = string.Empty;

    [JsonPropertyName("StatusMessage")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string StatusMessage { get; init; } = string.Empty;
}
