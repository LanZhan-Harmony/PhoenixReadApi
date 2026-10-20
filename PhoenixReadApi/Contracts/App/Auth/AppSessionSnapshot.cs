using System.Text.Json.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Auth;

/// <summary>客户端会话的本地备份内容。</summary>
internal sealed class AppSessionSnapshot
{
    [JsonPropertyName("cookies")]
    public IReadOnlyDictionary<string, string> Cookies { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    [JsonPropertyName("headers")]
    public IReadOnlyDictionary<string, string> Headers { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    [JsonPropertyName("verification_ticket")]
    public string VerificationTicket { get; init; } = string.Empty;

    [JsonPropertyName("masked_phone")]
    public string MaskedPhone { get; init; } = string.Empty;

    [JsonPropertyName("retry_after_seconds")]
    public int RetryAfterSeconds { get; init; }

    [JsonPropertyName("updated_at_unix_seconds")]
    public long UpdatedAtUnixSeconds { get; init; }

    [JsonPropertyName("cdid")]
    public string Cdid { get; init; } = string.Empty;

    [JsonPropertyName("klink_egdi")]
    public string KlinkEgdi { get; init; } = string.Empty;

    [JsonPropertyName("passport_csrf_token")]
    public string PassportCsrfToken { get; init; } = string.Empty;

    [JsonPropertyName("ttreq")]
    public string TtReq { get; init; } = string.Empty;

    [JsonPropertyName("device_id")]
    public string DeviceId { get; init; } = string.Empty;

    [JsonPropertyName("install_id")]
    public string InstallId { get; init; } = string.Empty;

    [JsonPropertyName("user_id")]
    public string UserId { get; init; } = string.Empty;

    [JsonPropertyName("user_name")]
    public string UserName { get; init; } = string.Empty;

    [JsonPropertyName("user_avatar_url")]
    public string UserAvatarUrl { get; init; } = string.Empty;

    [JsonPropertyName("user_description")]
    public string UserDescription { get; init; } = string.Empty;

    [JsonPropertyName("user_masked_phone")]
    public string UserMaskedPhone { get; init; } = string.Empty;
}
