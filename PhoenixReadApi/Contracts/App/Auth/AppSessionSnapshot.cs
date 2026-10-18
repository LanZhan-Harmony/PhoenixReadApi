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
}
