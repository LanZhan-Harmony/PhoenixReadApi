using System.Text.Json.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Details;

internal sealed class AppMultiDetailItem
{
    [JsonPropertyName("book_id")]
    public string BookId { get; init; } = string.Empty;

    [JsonPropertyName("book_name")]
    public string BookName { get; init; } = string.Empty;

    [JsonPropertyName("abstract")]
    public string Abstract { get; init; } = string.Empty;

    [JsonPropertyName("book_abstract_v2")]
    public string BookAbstractV2 { get; init; } = string.Empty;

    [JsonPropertyName("thumb_url")]
    public string ThumbUrl { get; init; } = string.Empty;

    [JsonPropertyName("expand_thumb_url")]
    public string ExpandThumbUrl { get; init; } = string.Empty;

    [JsonPropertyName("bookshelf_thumb_url")]
    public string BookshelfThumbUrl { get; init; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; init; } = string.Empty;

    [JsonPropertyName("chapter_number")]
    public string ChapterNumber { get; init; } = string.Empty;

    [JsonPropertyName("serial_count")]
    public string SerialCount { get; init; } = string.Empty;

    [JsonPropertyName("drama_chapter_number")]
    public string DramaChapterNumber { get; init; } = string.Empty;

    [JsonPropertyName("score")]
    public string Score { get; init; } = string.Empty;

    [JsonPropertyName("final_score_v2")]
    public string FinalScoreV2 { get; init; } = string.Empty;

    [JsonPropertyName("author")]
    public string Author { get; init; } = string.Empty;

    [JsonPropertyName("sub_info")]
    public string SubInfo { get; init; } = string.Empty;

    [JsonPropertyName("first_chapter_item_id")]
    public string FirstChapterItemId { get; init; } = string.Empty;

    [JsonPropertyName("last_chapter_item_id")]
    public string LastChapterItemId { get; init; } = string.Empty;

    [JsonPropertyName("tags")]
    public string Tags { get; init; } = string.Empty;
}
