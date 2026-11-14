using System.Globalization;
using PhoenixReadApi.Internal.Contracts.Shared.Drama;
using PhoenixReadApi.Internal.Infrastructure.Web;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Features.Shared;

using Drama = global::PhoenixReadApi.Drama;
using User = global::PhoenixReadApi.User;

/// <summary>将红果接口返回的剧集记录映射为稳定的公共模型。</summary>
internal static class DramaMapper
{
    internal static Drama FromDto(
        DramaDto value,
        string category,
        DateTimeOffset? now = null,
        string? scoreOverride = null
    )
    {
        string sourceId = ValueNormalizer.First(
            value.SeriesId,
            value.SeriesIdNumber,
            value.BookId,
            value.Keyword,
            value.VideoDetail?.SeriesId
        );
        if (!ValueNormalizer.IsNumericId(sourceId))
        {
            return new Drama();
        }

        string title = ValueNormalizer.First(
            value.SeriesTitle,
            value.SeriesName,
            value.Title,
            value.Name,
            value.VideoDetail?.SeriesTitle,
            value.VideoDetail?.Title,
            sourceId
        );
        string remark = ValueNormalizer.First(value.EpisodeRemark);
        List<string> tags = DtoTags(value);
        string genre = ValueNormalizer.First(
            value.CategoryName,
            value.CategoryNameCamel,
            value.Category,
            tags.FirstOrDefault() ?? category
        );
        int episodeCount = Math.Max(
            value.EpisodeCount,
            Math.Max(
                value.EpisodeCountAlias,
                Math.Max(
                    value.DramaChapterNumber,
                    Math.Max(value.ChapterNumber, value.VideoDetail?.EpisodeCount ?? 0)
                )
            )
        );
        if (remark.Length == 0 && episodeCount > 0)
        {
            remark = "共" + episodeCount + "集";
        }

        string desc = ValueNormalizer.First(
            value.SeriesIntro,
            value.VideoDescription,
            value.Abstract,
            value.Intro,
            value.Description
        );
        string cover = ValueNormalizer.First(
            value.SeriesCover,
            value.Cover,
            value.CoverUrl,
            value.ThumbUrl,
            value.ExpandThumbUrl,
            value.VideoDetail?.CoverUrl
        );
        string score = ValueNormalizer.First(
            value.FinalScoreV2,
            value.FinalScore,
            value.Score,
            scoreOverride
        );
        string status = ValueNormalizer.First(value.SeriesStatus, value.Status);

        long playCount = 0;
        string playCountStr = ValueNormalizer.First(value.SeriesPlayCount, value.PlayCount);
        if (
            long.TryParse(
                playCountStr,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out long parsedPlay
            )
        )
        {
            playCount = parsedPlay;
        }

        string novelId = value.RelateBook?.BookInfo?.BookId ?? string.Empty;
        string novelTitle = ValueNormalizer.First(
            value.RelateBook?.BookInfo?.BookName,
            value.RelateBook?.BookInfo?.BookShortName
        );

        return new Drama
        {
            Id = sourceId,
            Title = title,
            Description = desc,
            CoverUrl = cover,
            Category = genre,
            Remark = remark,
            EpisodeCount = Math.Max(0, episodeCount),
            Tags = tags,
            ReleaseStatus = ReleaseStatusFromValue(status, remark, tags),
            Score = score,
            Views = ValueNormalizer.First(value.SeriesPlayCount, value.PlayCount),
            Heat = DtoHeat(value),
            OnlineDate = DtoOnlineDate(value, now ?? DateTimeOffset.Now),
            Credits = DtoCredits(value.MainCreatorUsers, value.Celebrities),
            IsMultiSeason =
                value.IsMultiSeason || !string.IsNullOrWhiteSpace(value.EffectiveAlbumId),
            SeasonIndex = value.SeasonIndex > 0 ? value.SeasonIndex : 1,
            AlbumId = value.EffectiveAlbumId,
            NovelId = novelId,
            NovelTitle = novelTitle,
            PlayCount = playCount,
            FollowCount = value.FollowedCount,
            LikeCount = value.DiggCount,
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
            Title = ValueNormalizer.First(title, videoId),
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
            Title = ValueNormalizer.First(title, seriesId),
            Description = description,
            CoverUrl = coverUrl,
            EpisodeCount = Math.Max(0, episodeCount),
            ReleaseStatus = ReleaseStatusFromValue(status, tags: tags),
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

    internal static DramaReleaseStatus ReleaseStatusFromValue(
        string value,
        string remark = "",
        IReadOnlyList<string>? tags = null
    )
    {
        value = (value ?? string.Empty).Trim().ToLowerInvariant();
        if (value is "1" or "finished" or "complete" or "completed" or "end")
        {
            return DramaReleaseStatus.Finished;
        }
        if (value is "0" or "ongoing" or "serializing" or "updating")
        {
            return DramaReleaseStatus.Ongoing;
        }

        if (!string.IsNullOrWhiteSpace(remark))
        {
            string cleanRemark = remark.Trim();
            if (
                cleanRemark.Contains("完结", StringComparison.OrdinalIgnoreCase)
                || cleanRemark.Contains("全集", StringComparison.OrdinalIgnoreCase)
                || (cleanRemark.StartsWith('全') && cleanRemark.EndsWith('集'))
            )
            {
                return DramaReleaseStatus.Finished;
            }

            if (
                cleanRemark.Contains("更新", StringComparison.OrdinalIgnoreCase)
                || cleanRemark.Contains("连载", StringComparison.OrdinalIgnoreCase)
            )
            {
                return DramaReleaseStatus.Ongoing;
            }
        }

        if (tags is not null)
        {
            foreach (string tag in tags)
            {
                if (string.IsNullOrWhiteSpace(tag))
                {
                    continue;
                }

                if (
                    tag.Contains("完结", StringComparison.OrdinalIgnoreCase)
                    || tag.Contains("全集", StringComparison.OrdinalIgnoreCase)
                )
                {
                    return DramaReleaseStatus.Finished;
                }

                if (
                    tag.Contains("更新", StringComparison.OrdinalIgnoreCase)
                    || tag.Contains("连载", StringComparison.OrdinalIgnoreCase)
                )
                {
                    return DramaReleaseStatus.Ongoing;
                }
            }
        }

        return DramaReleaseStatus.Unknown;
    }

    private static List<DramaCredit> DtoCredits(
        IReadOnlyList<DramaCreatorUserDto> creators,
        IReadOnlyList<DramaCelebrityDto> celebrities
    )
    {
        List<DramaCredit> credits = [];
        Dictionary<string, int> positions = [with(StringComparer.Ordinal)];
        foreach (DramaCreatorUserDto? creator in creators ?? [])
        {
            if (creator is null)
            {
                continue;
            }

            // The service occasionally omits nested account objects for a
            // partially populated creator. Keep that item usable instead of
            // letting one incomplete credit abort the whole detail response.
            DramaCreatorBaseInfoDto? baseInfo = creator.BaseInfo;
            DramaCreatorTagDto userTag = creator.UserTag;
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

            string roleName = celebrity.RoleName ?? string.Empty;
            Add(
                ProfileId(celebrity.Schema ?? string.Empty),
                celebrity.Id ?? string.Empty,
                celebrity.Name ?? string.Empty,
                celebrity.Introduction ?? string.Empty,
                celebrity.AvatarUrl ?? string.Empty,
                celebrity.RoleType,
                [
                    ValueNormalizer.First(
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
            string id = ValueNormalizer.First(primaryId, fallbackId).Trim();
            if (!ValueNormalizer.IsNumericId(id))
            {
                return;
            }

            List<DramaCreditRole> roles = ParseRoles(labels, roleType);
            User user = new()
            {
                Id = id,
                Name = name ?? string.Empty,
                Introduction = introduction ?? string.Empty,
                AvatarUrl = avatarUrl ?? string.Empty,
            };
            if (positions.TryGetValue(id, out int position))
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
                Name = ValueNormalizer.First(first.User.Name, user.Name),
                Introduction = ValueNormalizer.First(first.User.Introduction, user.Introduction),
                AvatarUrl = ValueNormalizer.First(first.User.AvatarUrl, user.AvatarUrl),
            },
            Roles = MergeRoles(first.Roles, roles),
        };

    private static List<DramaCreditRole> MergeRoles(
        IReadOnlyList<DramaCreditRole> first,
        IReadOnlyList<DramaCreditRole> second
    )
    {
        List<DramaCreditRole> result = [];
        Dictionary<string, int> positions = [with(StringComparer.Ordinal)];
        foreach (DramaCreditRole? role in first.Concat(second))
        {
            if (role is null)
            {
                continue;
            }

            string key = role.CharacterName + "|" + role.Label;
            if (positions.TryGetValue(key, out int position))
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
        HashSet<string> seen = [with(StringComparer.Ordinal)];
        foreach (var raw in labels ?? [])
        {
            string label = (raw ?? string.Empty).Trim();
            if (label.Length == 0 || !seen.Add(label))
            {
                continue;
            }

            DramaCreditRoleType type = label switch
            {
                _ when label.StartsWith('饰') => providerRoleType switch
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
            string character = type
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
        int marker = schema.IndexOf("uid=", StringComparison.OrdinalIgnoreCase);
        if (marker < 0)
        {
            return string.Empty;
        }

        int start = marker + 4;
        int end = schema.IndexOf('&', start);
        string value = schema[start..(end < 0 ? schema.Length : end)];
        value = Uri.UnescapeDataString(value).Trim();
        return ValueNormalizer.IsNumericId(value) ? value : string.Empty;
    }

    private static List<string> DtoTags(DramaDto data)
    {
        List<string> tags = [];
        HashSet<string> seen = [with(StringComparer.Ordinal)];
        foreach (DramaTagDto item in data.Tags ?? [])
        {
            if (!string.IsNullOrWhiteSpace(item.Name) && seen.Add(item.Name.Trim()))
            {
                tags.Add(item.Name.Trim());
            }
        }
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
        foreach (string item in data.CategorySchema ?? [])
        {
            if (!string.IsNullOrWhiteSpace(item) && seen.Add(item.Trim()))
            {
                tags.Add(item.Trim());
            }
        }
        return tags;
    }

    private static string DtoHeat(DramaDto data)
    {
        string score = ValueNormalizer.First(data.HotScoreData?.Score, data.HotScore);
        return
            score.Length > 0
            && double.TryParse(
                score,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double value
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
                out long number
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
            string label = item.Content ?? string.Empty;
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
                string date = DateNormalizer.Normalize(label);
                if (date.Length > 0)
                {
                    return date;
                }
            }
        }
        return string.Empty;
    }
}
