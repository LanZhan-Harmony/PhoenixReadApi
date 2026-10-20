using System.Text.Json.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Comment;

internal sealed class AppSharkParameter
{
    [JsonPropertyName("enter_from")]
    public string EnterFrom { get; init; } = "ShortSeriesActivity";

    [JsonPropertyName("previous_page")]
    public string PreviousPage { get; init; } = "MainFragmentActivity";

    [JsonPropertyName("page_list")]
    public string PageList { get; init; } = "MainFragmentActivity,ShortSeriesActivity";
}
