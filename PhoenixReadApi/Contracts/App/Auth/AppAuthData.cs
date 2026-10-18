using System.Text.Json.Serialization;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Auth;

internal sealed class AppLoginData
{
    [JsonPropertyName("user_id_str")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string UserId { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("screen_name")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string ScreenName { get; init; } = string.Empty;

    [JsonPropertyName("avatar_url")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string AvatarUrl { get; init; } = string.Empty;

    [JsonPropertyName("description")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Description { get; init; } = string.Empty;

    [JsonPropertyName("mobile")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Mobile { get; init; } = string.Empty;

    [JsonPropertyName("mobile_ticket")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string MobileTicket { get; init; } = string.Empty;

    [JsonPropertyName("retry_time")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int RetryTime { get; init; }

    [JsonPropertyName("log_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string LogId { get; init; } = string.Empty;

    [JsonPropertyName("mfa_data")]
    public AppMfaData Mfa { get; init; } = new();
}

internal sealed class AppMfaData
{
    [JsonPropertyName("sms_code_key")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string SmsCodeKey { get; init; } = string.Empty;

    [JsonPropertyName("verify_ticket")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string VerifyTicket { get; init; } = string.Empty;

    [JsonPropertyName("verify_reason")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string VerifyReason { get; init; } = string.Empty;

    [JsonPropertyName("verify_ways")]
    public IReadOnlyList<AppVerifyWay> VerifyWays { get; init; } = [];
}

internal sealed class AppVerifyWay
{
    [JsonPropertyName("verify_scene")]
    public string Scene { get; init; } = string.Empty;

    [JsonPropertyName("verify_type")]
    public string Type { get; init; } = string.Empty;
}
