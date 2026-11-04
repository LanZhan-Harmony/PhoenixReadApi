using System.Text.Json.Serialization;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Catalog;

/// <summary>原著小说/书籍分类落地页响应模型。</summary>
internal sealed class AppNovelCategoryFrontResponse
{
    [JsonPropertyName("code")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Code { get; init; } = string.Empty;

    [JsonPropertyName("message")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Message { get; init; } = string.Empty;

    [JsonPropertyName("data")]
    public AppNovelCategoryFrontData Data { get; init; } = new();
}

internal sealed class AppNovelCategoryFrontData
{
    [JsonPropertyName("category_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string CategoryId { get; init; } = string.Empty;

    [JsonPropertyName("category_name")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string CategoryName { get; init; } = string.Empty;

    [JsonPropertyName("book_list")]
    public IReadOnlyList<AppNovelBookDto> BookList { get; init; } = [];

    [JsonPropertyName("has_more")]
    public bool HasMore { get; init; }
}

internal sealed class AppNovelBookDto
{
    [JsonPropertyName("book_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string BookId { get; init; } = string.Empty;

    [JsonPropertyName("book_name")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string BookName { get; init; } = string.Empty;

    [JsonPropertyName("author")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Author { get; init; } = string.Empty;

    [JsonPropertyName("abstract")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Description { get; init; } = string.Empty;

    [JsonPropertyName("thumb_url")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Cover { get; init; } = string.Empty;

    [JsonPropertyName("creation_status")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string CreationStatus { get; init; } = string.Empty;

    [JsonPropertyName("read_count")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long ReadCount { get; init; }

    [JsonPropertyName("tags")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Tags { get; init; } = string.Empty;
}
