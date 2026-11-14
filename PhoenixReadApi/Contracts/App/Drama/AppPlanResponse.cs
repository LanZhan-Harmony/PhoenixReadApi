using System.Text.Json.Serialization;
using PhoenixReadApi.Internal.Contracts.Shared.Drama;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Drama;

/// <summary>红果通用 Plan Feed (bookapi/plan/v1) 响应模型。</summary>
internal sealed class AppPlanResponse
{
    [JsonPropertyName("code")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Code { get; init; } = string.Empty;

    [JsonPropertyName("message")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Message { get; init; } = string.Empty;

    [JsonPropertyName("BaseResp")]
    public AppBaseResponse BaseResponse { get; init; } = new();

    [JsonPropertyName("session_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string SessionId { get; init; } = string.Empty;

    [JsonPropertyName("data")]
    public IReadOnlyList<AppPlanCellDto> Data { get; init; } = [];
}

/// <summary>Plan Feed 单元格模型。</summary>
internal sealed class AppPlanCellDto
{
    [JsonPropertyName("show_type")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int ShowType { get; init; }

    [JsonPropertyName("cell_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string CellId { get; init; } = string.Empty;

    [JsonPropertyName("cell_name")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string CellName { get; init; } = string.Empty;

    [JsonPropertyName("session_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string SessionId { get; init; } = string.Empty;

    [JsonPropertyName("video_data")]
    [JsonConverter(typeof(FlexibleDramaListConverter))]
    public IReadOnlyList<DramaDto> VideoData { get; init; } = [];

    [JsonPropertyName("cell_data")]
    [JsonConverter(typeof(FlexibleDramaListConverter))]
    public IReadOnlyList<DramaDto> CellData { get; init; } = [];

    [JsonIgnore]
    public IReadOnlyList<DramaDto> EffectiveItems =>
        VideoData?.Count > 0 ? VideoData : CellData ?? [];
}
