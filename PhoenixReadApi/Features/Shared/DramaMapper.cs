using System.Globalization;
using System.Text.Json;
using PhoenixReadApi.Internal.Contracts.App.Catalog;
using PhoenixReadApi.Internal.Infrastructure.Web;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Features.Shared;

/// <summary>将红果接口返回的剧集记录映射为稳定的公共模型。</summary>
internal static class DramaMapper
{
    private static readonly char[] separator = [',', '/', '，', '、'];

    internal static Drama FromDto(AppDramaDto value, string category, DateTimeOffset? now = null)
    {
        AppDramaDto data = value.VideoData ?? value;
        var sourceId = JsonValueReader.First(data.SeriesId, data.SeriesIdNumber, data.Keyword);
        if (!JsonValueReader.IsNumericId(sourceId))
        {
            return new Drama();
        }

        var title = JsonValueReader.First(
            data.SeriesTitle,
            data.SeriesName,
            data.Title,
            data.Name,
            sourceId
        );
        var remark = JsonValueReader.First(data.EpisodeRemark);
        List<string> tags = DtoTags(data);
        var genre = JsonValueReader.First(
            data.CategoryName,
            data.CategoryNameCamel,
            data.Category,
            tags.FirstOrDefault() ?? category
        );
        if (remark.Length == 0 && data.EpisodeCount > 0)
        {
            remark = "共" + data.EpisodeCount + "集";
        }

        return new Drama
        {
            Id = "hongguo:" + sourceId,
            Source = "hongguo",
            SourceId = sourceId,
            Title = title,
            Name = title,
            Description = JsonValueReader.First(data.SeriesIntro, data.VideoDescription),
            CoverUrl = JsonValueReader.First(data.SeriesCover, data.Cover),
            Category = genre,
            Channel = "红果",
            Remark = remark,
            EpisodeCount = Math.Max(0, data.EpisodeCount),
            Tags = tags,
            ReleaseStatus = data.SeriesStatus switch
            {
                "1" => "finished",
                "0" => "ongoing",
                _ => StatusFromRemark(remark),
            },
            Score = data.Score ?? string.Empty,
            Views = JsonValueReader.First(data.SeriesPlayCount, data.PlayCount),
            Heat = DtoHeat(data),
            OnlineDate = DtoOnlineDate(data, now ?? DateTimeOffset.Now),
        };
    }

    internal static Drama FromJson(JsonElement value, string category)
    {
        if (value.ValueKind != JsonValueKind.Object)
        {
            return new Drama();
        }

        JsonElement data = JsonValueReader.Object(value, "video_data");
        if (data.ValueKind != JsonValueKind.Object)
        {
            data = value;
        }

        var sourceId = JsonValueReader.First(
            JsonValueReader.String(data, "series_id_str", "series_id"),
            JsonValueReader.String(value, "series_id_str", "series_id"),
            JsonValueReader.String(data, "keyword"),
            JsonValueReader.String(value, "keyword")
        );
        if (!JsonValueReader.IsNumericId(sourceId))
        {
            return new Drama();
        }

        var title = JsonValueReader.First(
            JsonValueReader.String(data, "series_title", "series_name", "title"),
            JsonValueReader.String(value, "series_name", "name"),
            sourceId
        );
        var intro = JsonValueReader.First(
            JsonValueReader.String(data, "series_intro", "video_desc"),
            JsonValueReader.String(value, "series_intro")
        );
        var cover = JsonValueReader.First(
            JsonValueReader.String(data, "series_cover", "cover"),
            JsonValueReader.String(value, "series_cover")
        );
        var count = JsonValueReader.First(
            JsonValueReader.String(data, "episode_cnt"),
            JsonValueReader.String(value, "episode_cnt")
        );
        var remark = JsonValueReader.First(
            JsonValueReader.String(data, "episode_right_text"),
            JsonValueReader.String(value, "episode_right_text")
        );
        List<string> tags = JsonValueReader.StringList(data, "tags");
        foreach (JsonElement item in JsonValueReader.Array(data, "category_list"))
        {
            var name = JsonValueReader.String(item, "name");
            if (name.Length > 0 && !tags.Contains(name, StringComparer.Ordinal))
            {
                tags.Add(name);
            }
        }

        var schema = JsonValueReader.String(data, "category_schema");
        if (schema.Length > 0)
        {
            try
            {
                using JsonDocument schemaDocument = JsonDocument.Parse(schema);
                foreach (JsonElement item in schemaDocument.RootElement.EnumerateArray())
                {
                    var name = JsonValueReader.String(item, "name");
                    if (name.Length > 0 && !tags.Contains(name, StringComparer.Ordinal))
                    {
                        tags.Add(name);
                    }
                }
            }
            catch (JsonException)
            {
                // 可选标签解析失败时，不应影响其他字段有效的剧集。
            }
        }

        var genre = JsonValueReader.First(
            JsonValueReader.String(data, "category_name", "categoryName", "category"),
            tags.FirstOrDefault() ?? category
        );
        var status = JsonValueReader.String(data, "series_status") switch
        {
            "1" => "finished",
            "0" => "ongoing",
            _ => StatusFromRemark(remark),
        };
        if (remark.Length == 0 && count.Length > 0)
        {
            remark = "共" + count + "集";
        }

        var episodeCount = int.TryParse(
            count,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var parsedCount
        )
            ? parsedCount
            : 0;
        return new Drama
        {
            Id = "hongguo:" + sourceId,
            Source = PhoenixReadApi.SourceName,
            SourceId = sourceId,
            Title = title,
            Name = title,
            Description = intro,
            CoverUrl = cover,
            Category = genre,
            Channel = "红果",
            Remark = remark,
            EpisodeCount = episodeCount,
            Tags = tags,
            ReleaseStatus = status,
            Score = JsonValueReader.String(data, "score"),
            Views = JsonValueReader.String(data, "series_play_cnt", "play_cnt"),
            Heat = Heat(data),
            OnlineDate = OnlineDate(data, DateTimeOffset.Now),
        };
    }

    internal static string StatusFromRemark(string remark) =>
        remark.Contains("完结", StringComparison.OrdinalIgnoreCase)
        || remark.Contains("全集", StringComparison.OrdinalIgnoreCase)
            ? "finished"
        : remark.Contains("更新", StringComparison.OrdinalIgnoreCase)
        || remark.Contains("连载", StringComparison.OrdinalIgnoreCase)
            ? "ongoing"
        : string.Empty;

    private static List<string> DtoTags(AppDramaDto data)
    {
        List<string> tags = [];
        HashSet<string> seen = new(StringComparer.Ordinal);
        AddJson(data.Tags);
        foreach (AppCategoryDto item in data.CategoryList)
        {
            if (!string.IsNullOrWhiteSpace(item.Name) && seen.Add(item.Name.Trim()))
            {
                tags.Add(item.Name.Trim());
            }
        }
        foreach (var item in data.CategorySchema)
        {
            if (!string.IsNullOrWhiteSpace(item) && seen.Add(item.Trim()))
            {
                tags.Add(item.Trim());
            }
        }
        return tags;

        void AddJson(JsonElement value)
        {
            switch (value.ValueKind)
            {
                case JsonValueKind.String:
                    foreach (
                        var item in value
                            .GetString()!
                            .Split(
                                separator,
                                StringSplitOptions.RemoveEmptyEntries
                                    | StringSplitOptions.TrimEntries
                            )
                    )
                    {
                        if (seen.Add(item))
                        {
                            tags.Add(item);
                        }
                    }
                    break;
                case JsonValueKind.Array:
                    foreach (JsonElement item in value.EnumerateArray())
                    {
                        AddJson(item);
                    }

                    break;
                case JsonValueKind.Object:
                    var itemName = JsonValueReader.String(value, "name", "title", "label", "tag");
                    if (itemName.Length > 0 && seen.Add(itemName))
                    {
                        tags.Add(itemName);
                    }

                    break;
            }
        }
    }

    private static string DtoHeat(AppDramaDto data)
    {
        var score = JsonValueReader.First(data.HotScoreData?.Score, data.HotScore);
        return
            score.Length > 0
            && double.TryParse(
                score,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var value
            )
            && !double.IsNaN(value)
            && !double.IsInfinity(value)
            && value >= 0
            ? score
            : data.HotScoreData?.Text ?? string.Empty;
    }

    private static string DtoOnlineDate(AppDramaDto data, DateTimeOffset now)
    {
        if (
            long.TryParse(
                data.FirstVisibleTime,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var number
            )
        )
        {
            if (number > 10_000_000_000)
            {
                number /= 1000;
            }

            try
            {
                DateTimeOffset date = DateTimeOffset.FromUnixTimeSeconds(number);
                if (date.Year is >= 2000 and <= 2100)
                {
                    return date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                }
            }
            catch (ArgumentOutOfRangeException) { }
        }

        foreach (AppSubtitleDto item in data.Subtitles)
        {
            var label = item.Content ?? string.Empty;
            if (label == "今日上新")
            {
                return now.ToOffset(TimeSpan.FromHours(8))
                    .ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            }

            if (label == "昨日上新")
            {
                return now.ToOffset(TimeSpan.FromHours(8))
                    .AddDays(-1)
                    .ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            }

            if (label.EndsWith("上新", StringComparison.Ordinal))
            {
                var date = DateNormalizer.Normalize(label);
                if (date.Length > 0)
                {
                    return date;
                }
            }
        }
        return string.Empty;
    }

    private static string Heat(JsonElement row)
    {
        JsonElement hot = JsonValueReader.Object(row, "hot_score_data");
        var score = JsonValueReader.First(
            JsonValueReader.String(hot, "score"),
            JsonValueReader.String(row, "hot_score")
        );
        return
            score.Length > 0
            && double.TryParse(
                score,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var value
            )
            && !double.IsNaN(value)
            && !double.IsInfinity(value)
            && value >= 0
            ? score
            : JsonValueReader.String(hot, "text");
    }

    private static string OnlineDate(JsonElement row, DateTimeOffset now)
    {
        var timestamp = JsonValueReader.String(row, "first_visible_time");
        if (
            long.TryParse(
                timestamp,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var number
            )
        )
        {
            if (number > 10_000_000_000)
            {
                number /= 1000;
            }

            try
            {
                DateTimeOffset date = DateTimeOffset.FromUnixTimeSeconds(number);
                if (date.Year is >= 2000 and <= 2100)
                {
                    return date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                }
            }
            catch (ArgumentOutOfRangeException) { }
        }

        foreach (JsonElement item in JsonValueReader.Array(row, "sub_title_list"))
        {
            var label = JsonValueReader.String(item, "content");
            if (label == "今日上新")
            {
                return now.ToOffset(TimeSpan.FromHours(8))
                    .ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            }

            if (label == "昨日上新")
            {
                return now.ToOffset(TimeSpan.FromHours(8))
                    .AddDays(-1)
                    .ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            }

            if (label.EndsWith("上新", StringComparison.Ordinal))
            {
                var date = DateNormalizer.Normalize(label);
                if (date.Length > 0)
                {
                    return date;
                }
            }
        }

        return string.Empty;
    }
}
