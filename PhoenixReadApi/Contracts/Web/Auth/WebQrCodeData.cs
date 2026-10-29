using System.Text.Json.Serialization;

namespace PhoenixReadApi.Internal.Contracts.Web.Auth;

internal sealed class WebQrCodeData
{
    [JsonPropertyName("token")]
    public string Token { get; init; } = string.Empty;

    [JsonPropertyName("qrcode")]
    public string QrCode { get; init; } = string.Empty;

    [JsonPropertyName("expire_time")]
    public long ExpireTime { get; init; }
}

internal sealed class WebQrStatusData
{
    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("redirect_url")]
    public string RedirectUrl { get; init; } = string.Empty;

    [JsonPropertyName("error_code")]
    public int ErrorCode { get; init; }
}

internal sealed class WebPassportAccountInfoData
{
    [JsonPropertyName("user_id")]
    public long UserId { get; init; }

    [JsonPropertyName("user_id_str")]
    public string UserIdStr { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("screen_name")]
    public string ScreenName { get; init; } = string.Empty;

    [JsonPropertyName("avatar_url")]
    public string AvatarUrl { get; init; } = string.Empty;
}

internal sealed class WebPassportResponse<T>
{
    [JsonPropertyName("data")]
    public T? Data { get; init; }

    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;
}
