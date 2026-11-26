using System.Text.Json.Serialization;
using PhoenixReadApi.Internal.Contracts.App.Comment;
using PhoenixReadApi.Internal.Contracts.Shared.Drama;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Search;

/// <summary>短剧综合搜索 tab/v 接口的顶层响应。</summary>
internal sealed class AppSearchTabResponse
{
    [JsonPropertyName("code")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Code { get; init; } = string.Empty;

    [JsonPropertyName("message")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Message { get; init; } = string.Empty;

    [JsonPropertyName("BaseResp")]
    public AppBaseResponse BaseResponse { get; init; } = new();

    [JsonPropertyName("search_tabs")]
    public IReadOnlyList<AppSearchTabItem> SearchTabs { get; init; } = [];

    [JsonPropertyName("data")]
    public AppSearchTabData Data { get; init; } = new();
}

/// <summary>嵌套在 data 字段下的 search_tabs 容器。</summary>
internal sealed class AppSearchTabData
{
    [JsonPropertyName("search_tabs")]
    public IReadOnlyList<AppSearchTabItem> SearchTabs { get; init; } = [];
}

/// <summary>单个搜索 Tab 项（如短剧 Tab 29、小说 Tab 1、用户 Tab 27 等）。</summary>
internal sealed class AppSearchTabItem
{
    [JsonPropertyName("tab_type")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string TabType { get; init; } = string.Empty;

    [JsonPropertyName("search_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string SearchId { get; init; } = string.Empty;

    [JsonPropertyName("has_more")]
    public bool HasMore { get; init; }

    [JsonPropertyName("next_offset")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int NextOffset { get; init; }

    [JsonPropertyName("passback")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Passback { get; init; } = string.Empty;

    [JsonPropertyName("data")]
    public IReadOnlyList<AppSearchTabCell> Data { get; init; } = [];
}

/// <summary>搜索结果单元格，包含视频数据、小说、明星演职人员等列表。</summary>
internal sealed class AppSearchTabCell
{
    [JsonPropertyName("video_data")]
    [JsonConverter(typeof(FlexibleDramaListConverter))]
    public IReadOnlyList<DramaDto> VideoData { get; init; } = [];

    [JsonPropertyName("book_data")]
    [JsonConverter(typeof(FlexibleSearchBookListConverter))]
    public IReadOnlyList<AppSearchBookDto> BookData { get; init; } = [];

    [JsonPropertyName("celebrity_user_list")]
    public IReadOnlyList<AppSearchUserDto> CelebrityUserList { get; init; } = [];

    [JsonPropertyName("post_data")]
    public IReadOnlyList<AppSearchPostDto> PostData { get; init; } = [];
}

/// <summary>搜索结果中的原著小说 DTO。</summary>
internal sealed class AppSearchBookDto
{
    [JsonPropertyName("book_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string BookId { get; init; } = string.Empty;

    [JsonPropertyName("book_name")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string BookName { get; init; } = string.Empty;

    [JsonPropertyName("book_short_name")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string BookShortName { get; init; } = string.Empty;

    [JsonPropertyName("abstract")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Abstract { get; init; } = string.Empty;

    [JsonPropertyName("author")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Author { get; init; } = string.Empty;

    [JsonPropertyName("thumb_url")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string CoverUrl { get; init; } = string.Empty;

    [JsonPropertyName("serial_count")]
    [JsonConverter(typeof(FlexibleIntConverter))]
    public int SerialCount { get; init; }

    [JsonPropertyName("read_count")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long ReadCount { get; init; }

    [JsonPropertyName("creation_status")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string CreationStatus { get; init; } = string.Empty;

    [JsonPropertyName("tags")]
    [JsonConverter(typeof(DramaTagListConverter))]
    public IReadOnlyList<DramaTagDto> Tags { get; init; } = [];

    [JsonPropertyName("category_schema")]
    [JsonConverter(typeof(CategorySchemaConverter))]
    public IReadOnlyList<string> CategorySchema { get; init; } = [];

    [JsonPropertyName("category")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Category { get; init; } = string.Empty;

    [JsonPropertyName("genre")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Genre { get; init; } = string.Empty;

    [JsonPropertyName("book_type")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string BookType { get; init; } = string.Empty;

    [JsonPropertyName("audio_thumb_uri")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string AudioThumbUri { get; init; } = string.Empty;

    [JsonPropertyName("audio_thumb_url_hd")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string AudioThumbUrlHd { get; init; } = string.Empty;

    [JsonPropertyName("listen_count")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long ListenCount { get; init; }

    [JsonPropertyName("score")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Score { get; init; } = string.Empty;
}

/// <summary>搜索结果中的明星、创作者或演员用户 DTO。</summary>
internal sealed class AppSearchUserDto
{
    [JsonPropertyName("user_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string UserId { get; init; } = string.Empty;

    [JsonPropertyName("base_info")]
    public AppSearchUserBaseInfo BaseInfo { get; init; } = new();
}

internal sealed class AppSearchUserBaseInfo
{
    [JsonPropertyName("user_name")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string UserName { get; init; } = string.Empty;

    [JsonPropertyName("description")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Description { get; init; } = string.Empty;

    [JsonPropertyName("avatar_url")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string AvatarUrl { get; init; } = string.Empty;

    [JsonPropertyName("user_avatar")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string UserAvatar { get; init; } = string.Empty;

    [JsonIgnore]
    public string EffectiveAvatarUrl =>
        !string.IsNullOrWhiteSpace(UserAvatar) ? UserAvatar : AvatarUrl;
}

/// <summary>搜索讨论 Tab 返回的图文帖子 DTO。</summary>
internal sealed class AppSearchPostDto
{
    [JsonPropertyName("post_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string PostId { get; init; } = string.Empty;

    [JsonPropertyName("title")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("pure_content")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string PureContent { get; init; } = string.Empty;

    [JsonPropertyName("content")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Content { get; init; } = string.Empty;

    [JsonPropertyName("cover_url")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string CoverUrl { get; init; } = string.Empty;

    [JsonPropertyName("create_time")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long CreateTime { get; init; }

    [JsonPropertyName("reply_cnt")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long ReplyCount { get; init; }

    [JsonPropertyName("digg_cnt")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long DiggCount { get; init; }

    [JsonPropertyName("favorite_cnt")]
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long FavoriteCount { get; init; }

    [JsonPropertyName("has_digg")]
    public bool HasDigg { get; init; }

    [JsonPropertyName("has_favorite")]
    public bool HasFavorite { get; init; }

    [JsonPropertyName("tags")]
    [JsonConverter(typeof(DramaTagListConverter))]
    public IReadOnlyList<DramaTagDto> Tags { get; init; } = [];

    [JsonPropertyName("user_info")]
    public AppSearchPostUserDto UserInfo { get; init; } = new();

    [JsonPropertyName("book_card")]
    [JsonConverter(typeof(FlexibleDramaListConverter))]
    public IReadOnlyList<DramaDto> BookCards { get; init; } = [];
}

/// <summary>讨论帖子中的发帖用户 DTO（扁平结构）。</summary>
internal sealed class AppSearchPostUserDto
{
    [JsonPropertyName("user_id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string UserId { get; init; } = string.Empty;

    [JsonPropertyName("user_name")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string UserName { get; init; } = string.Empty;

    [JsonPropertyName("user_avatar")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string UserAvatar { get; init; } = string.Empty;
}
