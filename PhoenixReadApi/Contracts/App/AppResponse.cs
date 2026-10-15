using System.Text.Json.Serialization;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App;

/// <summary>Common envelope returned by Phoenix App endpoints.</summary>
internal sealed class AppResponse<T>
{
    [JsonPropertyName("code")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? Code { get; init; }

    [JsonPropertyName("data")]
    public T? Data { get; init; }

    [JsonPropertyName("message")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? Message { get; init; }

    [JsonPropertyName("BaseResp")]
    public AppBaseResponse? BaseResponse { get; init; }
}

internal sealed class AppBaseResponse
{
    [JsonPropertyName("StatusCode")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? StatusCode { get; init; }

    [JsonPropertyName("StatusMessage")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? StatusMessage { get; init; }
}
