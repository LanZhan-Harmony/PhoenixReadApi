using System.Globalization;
using System.Text.Json;
using PhoenixReadApi.Internal.Contracts.Shared.Drama;
using PhoenixReadApi.Internal.Infrastructure.Web;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Features.Shared;

/// <summary>将红果接口返回的剧集记录映射为稳定的公共模型。</summary>
internal static class DramaMapper
{
    private static readonly char[] separator = [',', '/', '，', '、'];

    internal static Drama FromDto(DramaDto value, string category, DateTimeOffset? now = null)
    {
        DramaDto data = value;
        var sourceId = JsonValueReader.First(data.SeriesId, data.SeriesIdNumber, data.Keyword);
        if (!JsonValueReader.IsNumericId(sourceId))
        {
            return new Drama();
        }

        var title = JsonValueReader.First(data.SeriesTitle, data.SeriesName, data.Title, sourceId);
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
            Id = sourceId,
            Title = title,
            Description = JsonValueReader.First(data.SeriesIntro, data.VideoDescription),
            CoverUrl = JsonValueReader.First(data.SeriesCover, data.Cover),
            Category = genre,
            Remark = remark,
            EpisodeCount = Math.Max(0, data.EpisodeCount),
            Tags = tags,
            ReleaseStatus = data.SeriesStatus switch
            {
                "1" => "finished",
                "0" => "ongoing",
                _ => StatusFromRemark(remark),
            },
            Score = data.Score,
            Views = JsonValueReader.First(data.SeriesPlayCount, data.PlayCount),
            Heat = DtoHeat(data),
            OnlineDate = DtoOnlineDate(data, now ?? DateTimeOffset.Now),
            Credits = DtoCredits(data.MainCreatorUsers, data.Celebrities),
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
        List<DramaCredit> credits = JsonCredits(data);
        return new Drama
        {
            Id = sourceId,
            Title = title,
            Description = intro,
            CoverUrl = cover,
            Category = genre,
            Remark = remark,
            EpisodeCount = episodeCount,
            Tags = tags,
            ReleaseStatus = status,
            Score = JsonValueReader.String(data, "score"),
            Views = JsonValueReader.String(data, "series_play_cnt", "play_cnt"),
            Heat = Heat(data),
            OnlineDate = OnlineDate(data, DateTimeOffset.Now),
            Credits = credits,
        };
    }

    internal static Drama FromRelatedDrama(
        string videoId,
        string title,
        string description,
        string coverUrl,
        int episodeCount
    ) =>
        new()
        {
            Id = videoId,
            Title = JsonValueReader.First(title, videoId),
            Description = description,
            CoverUrl = coverUrl,
            EpisodeCount = Math.Max(0, episodeCount),
        };

    internal static Drama FromShelf(
        string seriesId,
        string title,
        string description,
        string coverUrl,
        int episodeCount,
        string status,
        IReadOnlyList<string> tags
    ) =>
        new()
        {
            Id = seriesId,
            Title = JsonValueReader.First(title, seriesId),
            Description = description,
            CoverUrl = coverUrl,
            EpisodeCount = Math.Max(0, episodeCount),
            ReleaseStatus = status,
            Tags = tags ?? [],
        };

    internal static Drama FromAction(
        string seriesId,
        string title,
        string description,
        string coverUrl,
        int episodeCount,
        string status,
        IReadOnlyList<string> tags
    ) => FromShelf(seriesId, title, description, coverUrl, episodeCount, status, tags);

    internal static string StatusFromRemark(string remark) =>
        remark.Contains("完结", StringComparison.OrdinalIgnoreCase)
        || remark.Contains("全集", StringComparison.OrdinalIgnoreCase)
            ? "finished"
        : remark.Contains("更新", StringComparison.OrdinalIgnoreCase)
        || remark.Contains("连载", StringComparison.OrdinalIgnoreCase)
            ? "ongoing"
        : string.Empty;

    private static List<DramaCredit> DtoCredits(
        IReadOnlyList<DramaCreatorUserDto> creators,
        IReadOnlyList<DramaCelebrityDto> celebrities
    )
    {
        List<DramaCredit> credits = [];
        Dictionary<string, int> positions = new(StringComparer.Ordinal);
        foreach (DramaCreatorUserDto? creator in creators ?? [])
        {
            if (creator is null)
            {
                continue;
            }

            // The service occasionally omits nested account objects for a
            // partially populated creator. Keep that item usable instead of
            // letting one incomplete credit abort the whole detail response.
            var baseInfo = creator.BaseInfo;
            var userTag = creator.UserTag;
            Add(
                creator.UserId ?? string.Empty,
                baseInfo?.UserId ?? string.Empty,
                baseInfo?.Name ?? string.Empty,
                baseInfo?.Description ?? string.Empty,
                baseInfo?.AvatarUrl ?? string.Empty,
                -1,
                userTag
                    ?.Subtitles?.Where(static item => item is not null)
                    .Select(static item => item?.Text ?? string.Empty)
                    ?? []
            );
        }

        foreach (DramaCelebrityDto? celebrity in celebrities ?? [])
        {
            if (celebrity is null)
            {
                continue;
            }

            var roleName = celebrity.RoleName ?? string.Empty;
            Add(
                ProfileId(celebrity.Schema ?? string.Empty),
                celebrity.Id ?? string.Empty,
                celebrity.Name ?? string.Empty,
                celebrity.Introduction ?? string.Empty,
                celebrity.AvatarUrl ?? string.Empty,
                celebrity.RoleType,
                [
                    JsonValueReader.First(
                        celebrity.SubTitle,
                        roleName.Length > 0 ? "饰 " + roleName : string.Empty
                    ),
                ]
            );
        }

        return credits;

        void Add(
            string primaryId,
            string fallbackId,
            string name,
            string introduction,
            string avatarUrl,
            int roleType,
            IEnumerable<string> labels
        )
        {
            var id = JsonValueReader.First(primaryId, fallbackId).Trim();
            if (!JsonValueReader.IsNumericId(id))
            {
                return;
            }

            List<DramaCreditRole> roles = ParseRoles(labels, roleType);
            var user = new User
            {
                Id = id,
                Name = name ?? string.Empty,
                Introduction = introduction ?? string.Empty,
                AvatarUrl = avatarUrl ?? string.Empty,
            };
            if (positions.TryGetValue(id, out var position))
            {
                credits[position] = MergeCredit(credits[position], user, roles);
                return;
            }

            positions[id] = credits.Count;
            credits.Add(new DramaCredit { User = user, Roles = roles });
        }
    }

    private static List<DramaCredit> JsonCredits(JsonElement data)
    {
        List<DramaCredit> credits = [];
        Dictionary<string, int> positions = new(StringComparer.Ordinal);
        foreach (JsonElement item in JsonValueReader.Array(data, "main_creator_users"))
        {
            JsonElement baseInfo = JsonValueReader.Object(item, "base_info");
            JsonElement tag = JsonValueReader.Object(item, "user_tag");
            var userId = JsonValueReader.First(
                JsonValueReader.String(item, "user_id"),
                JsonValueReader.String(baseInfo, "user_id")
            );
            Add(
                userId,
                userId,
                JsonValueReader.String(baseInfo, "user_name"),
                JsonValueReader.String(baseInfo, "description"),
                JsonValueReader.String(baseInfo, "user_avatar"),
                -1,
                JsonValueReader
                    .Array(tag, "sub_title_list")
                    .Select(item => JsonValueReader.String(item, "text"))
            );
        }

        foreach (JsonElement item in JsonValueReader.Array(data, "celebrities"))
        {
            var schema = JsonValueReader.String(item, "schema");
            var roleName = JsonValueReader.String(item, "role_name");
            Add(
                ProfileId(schema),
                JsonValueReader.String(item, "celebrity_id"),
                JsonValueReader.String(item, "nickname", "name"),
                JsonValueReader.String(item, "intro", "introduction"),
                JsonValueReader.String(item, "avatar", "avatar_url"),
                JsonValueReader.Int(item, "role_type"),
                [
                    JsonValueReader.First(
                        JsonValueReader.String(item, "sub_title"),
                        roleName.Length > 0 ? "饰 " + roleName : string.Empty
                    ),
                ]
            );
        }

        return credits;

        void Add(
            string primaryId,
            string fallbackId,
            string name,
            string introduction,
            string avatarUrl,
            int roleType,
            IEnumerable<string> labels
        )
        {
            var id = JsonValueReader.First(primaryId, fallbackId).Trim();
            if (!JsonValueReader.IsNumericId(id))
            {
                return;
            }

            List<DramaCreditRole> roles = ParseRoles(labels, roleType);
            var user = new User
            {
                Id = id,
                Name = name,
                Introduction = introduction,
                AvatarUrl = avatarUrl,
            };
            if (positions.TryGetValue(id, out var position))
            {
                credits[position] = MergeCredit(credits[position], user, roles);
                return;
            }

            positions[id] = credits.Count;
            credits.Add(new DramaCredit { User = user, Roles = roles });
        }
    }

    private static DramaCredit MergeCredit(
        DramaCredit first,
        User user,
        IReadOnlyList<DramaCreditRole> roles
    ) =>
        new()
        {
            User = new User
            {
                Id = first.User.Id.Length > 0 ? first.User.Id : user.Id,
                Name = JsonValueReader.First(first.User.Name, user.Name),
                Introduction = JsonValueReader.First(first.User.Introduction, user.Introduction),
                AvatarUrl = JsonValueReader.First(first.User.AvatarUrl, user.AvatarUrl),
            },
            Roles = MergeRoles(first.Roles, roles),
        };

    private static List<DramaCreditRole> MergeRoles(
        IReadOnlyList<DramaCreditRole> first,
        IReadOnlyList<DramaCreditRole> second
    )
    {
        List<DramaCreditRole> result = [];
        Dictionary<string, int> positions = new(StringComparer.Ordinal);
        foreach (DramaCreditRole? role in first.Concat(second))
        {
            if (role is null)
            {
                continue;
            }

            var key = role.CharacterName + "|" + role.Label;
            if (positions.TryGetValue(key, out var position))
            {
                if (RoleSpecificity(role.Type) > RoleSpecificity(result[position].Type))
                {
                    result[position] = role;
                }
                continue;
            }

            positions[key] = result.Count;
            result.Add(role);
        }

        return result;
    }

    private static int RoleSpecificity(DramaCreditRoleType type) =>
        type switch
        {
            DramaCreditRoleType.Lead or DramaCreditRoleType.Supporting => 3,
            DramaCreditRoleType.Participant => 2,
            DramaCreditRoleType.Unknown => 0,
            _ => 1,
        };

    private static List<DramaCreditRole> ParseRoles(
        IEnumerable<string> labels,
        int providerRoleType = -1
    )
    {
        List<DramaCreditRole> roles = [];
        HashSet<string> seen = new(StringComparer.Ordinal);
        foreach (var raw in labels ?? [])
        {
            var label = (raw ?? string.Empty).Trim();
            if (label.Length == 0 || !seen.Add(label))
            {
                continue;
            }

            var type = label switch
            {
                _ when label.StartsWith("饰", StringComparison.Ordinal) => providerRoleType switch
                {
                    0 => DramaCreditRoleType.Lead,
                    1 => DramaCreditRoleType.Supporting,
                    4 => DramaCreditRoleType.Participant,
                    _ => DramaCreditRoleType.Participant,
                },
                "导演" => DramaCreditRoleType.Director,
                "编剧" => DramaCreditRoleType.Writer,
                "出品方" or "制作方" => DramaCreditRoleType.Producer,
                _ => DramaCreditRoleType.Other,
            };
            var character = type
                is DramaCreditRoleType.Lead
                    or DramaCreditRoleType.Supporting
                    or DramaCreditRoleType.Participant
                ? label[1..].Trim()
                : string.Empty;
            roles.Add(
                new DramaCreditRole
                {
                    Type = type,
                    CharacterName = character,
                    Label = label,
                }
            );
        }

        return roles;
    }

    private static string ProfileId(string schema)
    {
        schema ??= string.Empty;
        var marker = schema.IndexOf("uid=", StringComparison.OrdinalIgnoreCase);
        if (marker < 0)
        {
            return string.Empty;
        }

        var start = marker + 4;
        var end = schema.IndexOf('&', start);
        var value = schema[start..(end < 0 ? schema.Length : end)];
        value = Uri.UnescapeDataString(value).Trim();
        return JsonValueReader.IsNumericId(value) ? value : string.Empty;
    }

    private static List<string> DtoTags(DramaDto data)
    {
        List<string> tags = [];
        HashSet<string> seen = new(StringComparer.Ordinal);
        AddJson(data.Tags);
        foreach (DramaCategoryDto item in data.CategoryList ?? [])
        {
            if (
                item is not null
                && !string.IsNullOrWhiteSpace(item.Name)
                && seen.Add(item.Name.Trim())
            )
            {
                tags.Add(item.Name.Trim());
            }
        }
        foreach (var item in data.CategorySchema ?? [])
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

    private static string DtoHeat(DramaDto data)
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

    private static string DtoOnlineDate(DramaDto data, DateTimeOffset now)
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

        foreach (DramaSubtitleDto item in data.Subtitles ?? [])
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
