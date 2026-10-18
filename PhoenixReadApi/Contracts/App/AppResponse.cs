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
    public T? Data { get; init; }

    [JsonPropertyName("message")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Message { get; init; } = string.Empty;

    [JsonPropertyName("BaseResp")]
    public AppBaseResponse BaseResponse { get; init; } = new();

    // The progress-list API places these fields beside data instead of inside it.
    [JsonPropertyName("video_data")]
    public IReadOnlyList<AppReadProgressItem> VideoData { get; init; } = [];

    [JsonPropertyName("local_book_data")]
    public IReadOnlyList<AppReadProgressItem> LocalBookData { get; init; } = [];
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
