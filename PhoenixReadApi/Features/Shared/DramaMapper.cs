using System.Globalization;
using PhoenixReadApi.Internal.Contracts.Shared.Drama;
using PhoenixReadApi.Internal.Infrastructure.Web;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Features.Shared;

using Drama = PhoenixReadApi.Drama;
using User = PhoenixReadApi.User;

/// <summary>将红果接口返回的剧集记录映射为稳定的公共模型。</summary>
internal static class DramaMapper
{
    internal static Drama FromVideoDetail(
        DramaVideoDetailDto detail,
        string category,
        DateTimeOffset? now = null,
        string? scoreOverride = null
    ) =>
        detail is null
            ? new Drama()
            : FromDto(new DramaDto { VideoDetail = detail }, category, now, scoreOverride);

    internal static Drama FromDto(
        DramaDto value,
        string category,
        DateTimeOffset? now = null,
        string? scoreOverride = null
    )
    {
        DramaVideoDetailDto? detail = value.VideoDetail;

        string sourceId = ValueNormalizer.First(
            value.SeriesId,
            value.SeriesIdNumber,
            value.BookId,
            value.Keyword,
            detail?.SeriesIdStr,
            detail?.SeriesId
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
            detail?.SeriesTitle,
            detail?.SeriesName,
            detail?.Title,
            detail?.Name,
            sourceId
        );
        string remark = ValueNormalizer.First(value.EpisodeRemark, detail?.EpisodeRemark);
        List<string> tags = DtoTags(value);
        string genre = ValueNormalizer.First(
            value.CategoryName,
            value.CategoryNameCamel,
            value.Category,
            detail?.CategoryName,
            detail?.CategoryNameCamel,
            detail?.Category,
            tags.FirstOrDefault() ?? category
        );
        int episodeCount = Math.Max(
            Math.Max(value.EpisodeCount, value.EpisodeCountAlias),
            Math.Max(
                Math.Max(value.DramaChapterNumber, value.ChapterNumber),
                Math.Max(detail?.EpisodeCount ?? 0, detail?.EpisodeCountAlias ?? 0)
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
            value.Description,
            detail?.SeriesIntro,
            detail?.VideoDescription,
            detail?.Abstract,
            detail?.Intro,
            detail?.Description
        );
        string cover = ValueNormalizer.First(
            value.SeriesCover,
            value.Cover,
            value.CoverUrl,
            value.ThumbUrl,
            value.ExpandThumbUrl,
            detail?.SeriesCover,
            detail?.Cover,
            detail?.CoverUrl,
            detail?.ThumbUrl,
            detail?.ExpandThumbUrl
        );
        string score = ValueNormalizer.First(
            value.FinalScoreV2,
            value.FinalScore,
            value.Score,
            detail?.FinalScoreV2,
            detail?.FinalScore,
            detail?.Score,
            ExtractSecondaryScore(value.SecondaryInfos),
            ExtractSecondaryScore(detail?.SecondaryInfos),
            scoreOverride
        );
        string status = ValueNormalizer.First(
            value.SeriesStatus,
            value.Status,
            detail?.SeriesStatus,
            detail?.Status
        );
        string tagInfoText = ValueNormalizer.First(value.TagInfo?.Text, detail?.TagInfo?.Text);

        long playCount = 0;
        string playCountStr = ValueNormalizer.First(
            value.SeriesPlayCount,
            value.PlayCount,
            detail?.SeriesPlayCount,
            detail?.PlayCount
        );
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

        DramaRelateBookInfoDto? relateBook = !string.IsNullOrWhiteSpace(value.RelateBook?.BookInfo?.BookId)
            ? value.RelateBook.BookInfo
            : detail?.RelateBook?.BookInfo;
        string novelId = relateBook?.BookId ?? string.Empty;
        string novelTitle = ValueNormalizer.First(
            relateBook?.BookName,
            relateBook?.BookShortName
        );

        IReadOnlyList<DramaCreatorUserDto> creators = value.MainCreatorUsers is { Count: > 0 }
            ? value.MainCreatorUsers
            : detail?.MainCreatorUsers ?? [];
        IReadOnlyList<DramaCelebrityDto> celebrities = value.Celebrities is { Count: > 0 }
            ? value.Celebrities
            : detail?.Celebrities ?? [];

        string albumId = ValueNormalizer.First(value.EffectiveAlbumId, detail?.EffectiveAlbumId);
        if (albumId == "0")
        {
            albumId = string.Empty;
        }
        bool isMultiSeason =
            value.IsMultiSeason
            || (detail?.IsMultiSeason ?? false)
            || !string.IsNullOrWhiteSpace(albumId);
        int seasonIndex = ResolveSeasonIndex(value, detail);
        long followCount = value.FollowedCount > 0
            ? value.FollowedCount
            : (detail?.FollowedCount ?? 0);
        long likeCount = value.DiggCount > 0 ? value.DiggCount : (detail?.DiggCount ?? 0);

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
            ReleaseStatus = ReleaseStatusFromValue(status, remark, tags, tagInfoText),
            Score = score,
            Views = playCountStr,
            Heat = DtoHeat(value),
            OnlineDate = DtoOnlineDate(value, now ?? DateTimeOffset.Now),
            Credits = DtoCredits(creators, celebrities),
            IsMultiSeason = isMultiSeason,
            SeasonIndex = seasonIndex,
            AlbumId = albumId,
            NovelId = novelId,
            NovelTitle = novelTitle,
            PlayCount = playCount,
            FollowCount = followCount,
            LikeCount = likeCount,
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
        IReadOnlyList<string>? tags = null,
        string tagInfoText = ""
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
        if (value is "upcoming" or "subscribe" or "reservation")
        {
            return DramaReleaseStatus.Upcoming;
        }

        if (!string.IsNullOrWhiteSpace(remark))
        {
            DramaReleaseStatus fromRemark = StatusFromText(remark);
            if (fromRemark != DramaReleaseStatus.Unknown)
            {
                return fromRemark;
            }
        }

        if (tags is not null)
        {
            foreach (string tag in tags)
            {
                DramaReleaseStatus fromTag = StatusFromText(tag);
                if (fromTag != DramaReleaseStatus.Unknown)
                {
                    return fromTag;
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(tagInfoText))
        {
            string cleanTagInfo = tagInfoText.Trim();
            if (
                cleanTagInfo.Contains("待播", StringComparison.OrdinalIgnoreCase)
                || cleanTagInfo.Contains("即将上线", StringComparison.OrdinalIgnoreCase)
                || cleanTagInfo.Contains("预约", StringComparison.OrdinalIgnoreCase)
            )
            {
                return DramaReleaseStatus.Upcoming;
            }
        }

        return DramaReleaseStatus.Unknown;
    }

    private static DramaReleaseStatus StatusFromText(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return DramaReleaseStatus.Unknown;
        }

        string text = raw.Trim();
        if (
            text.Contains("完结", StringComparison.OrdinalIgnoreCase)
            || text.Contains("全集", StringComparison.OrdinalIgnoreCase)
            || (text.StartsWith('全') && text.EndsWith('集'))
        )
        {
            return DramaReleaseStatus.Finished;
        }

        if (
            text.Contains("更新", StringComparison.OrdinalIgnoreCase)
            || text.Contains("连载", StringComparison.OrdinalIgnoreCase)
        )
        {
            return DramaReleaseStatus.Ongoing;
        }

        if (
            text.Contains("待播", StringComparison.OrdinalIgnoreCase)
            || text.Contains("即将上线", StringComparison.OrdinalIgnoreCase)
            || text.Contains("预约", StringComparison.OrdinalIgnoreCase)
        )
        {
            return DramaReleaseStatus.Upcoming;
        }

        return DramaReleaseStatus.Unknown;
    }

    internal static List<DramaCredit> MapCredits(IReadOnlyList<DramaCelebrityDto> celebrities) =>
        DtoCredits([], celebrities);

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
        AddTagsFromSource(
            tags,
            data.Tags,
            data.CategoryList,
            data.CategorySchema,
            data.Subtitles,
            abstractTags: null,
            data.SecondaryInfos
        );

        if (data.VideoDetail is { } detail)
        {
            AddTagsFromSource(
                tags,
                detail.Tags,
                detail.CategoryList,
                detail.CategorySchema,
                detail.Subtitles,
                detail.AbstractTags,
                detail.SecondaryInfos
            );
        }

        return tags;
    }

    private static void AddTagsFromSource(
        List<string> tags,
        IReadOnlyList<DramaTagDto>? rawTags,
        IReadOnlyList<DramaCategoryDto>? categories,
        IReadOnlyList<string>? categorySchema,
        IReadOnlyList<DramaSubtitleDto>? subtitles,
        IReadOnlyList<DramaSubtitleDto>? abstractTags,
        IReadOnlyList<DramaSecondaryInfoDto>? secondaryInfos
    )
    {
        if (rawTags is not null)
        {
            foreach (DramaTagDto item in rawTags)
            {
                AddUniqueTag(tags, item?.Name);
            }
        }

        if (categories is not null)
        {
            foreach (DramaCategoryDto item in categories)
            {
                AddUniqueTag(tags, item?.Name);
            }
        }

        if (categorySchema is not null)
        {
            foreach (string item in categorySchema)
            {
                AddUniqueTag(tags, item);
            }
        }

        if (abstractTags is not null)
        {
            foreach (DramaSubtitleDto item in abstractTags)
            {
                if (item is not null && (item.DataType == 3 || item.DataType == 0))
                {
                    AddUniqueTag(tags, item.Content);
                }
            }
        }

        if (secondaryInfos is not null)
        {
            foreach (DramaSecondaryInfoDto item in secondaryInfos)
            {
                if (item is { DataType: 3 })
                {
                    AddUniqueTag(tags, item.Content);
                }
            }
        }

        if (subtitles is not null)
        {
            foreach (DramaSubtitleDto item in subtitles)
            {
                if (item is { DataType: 3 })
                {
                    AddUniqueTag(tags, item.Content);
                }
            }
        }
    }

    private static void AddUniqueTag(List<string> tags, string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return;
        }

        string tag = raw.Trim();
        if (!tags.Contains(tag, StringComparer.Ordinal))
        {
            tags.Add(tag);
        }
    }

    private static string ExtractSecondaryScore(IReadOnlyList<DramaSecondaryInfoDto>? infos)
    {
        if (infos is null)
        {
            return string.Empty;
        }

        foreach (DramaSecondaryInfoDto item in infos)
        {
            if (item is not { DataType: 4 } || string.IsNullOrWhiteSpace(item.Content))
            {
                continue;
            }

            string text = item.Content.Trim();
            if (text.EndsWith('分'))
            {
                text = text[..^1].Trim();
            }

            if (
                text.Length > 0
                && double.TryParse(
                    text,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out double score
                )
                && !double.IsNaN(score)
                && !double.IsInfinity(score)
                && score is > 0 and <= 10
            )
            {
                return text;
            }
        }

        return string.Empty;
    }

    private static int ResolveSeasonIndex(DramaDto value, DramaVideoDetailDto? detail)
    {
        if (value.SeasonIndex > 0)
        {
            return value.SeasonIndex;
        }

        if (detail is { SeasonIndex: > 0 })
        {
            return detail.SeasonIndex;
        }

        int fromSecondary = ExtractSecondarySeason(value.SecondaryInfos);
        if (fromSecondary > 0)
        {
            return fromSecondary;
        }

        fromSecondary = ExtractSecondarySeason(detail?.SecondaryInfos);
        return fromSecondary > 0 ? fromSecondary : 1;
    }

    private static int ExtractSecondarySeason(IReadOnlyList<DramaSecondaryInfoDto>? infos)
    {
        if (infos is null)
        {
            return 0;
        }

        foreach (DramaSecondaryInfoDto item in infos)
        {
            if (item is not { DataType: 43 } || string.IsNullOrWhiteSpace(item.Content))
            {
                continue;
            }

            string text = item.Content.Trim();
            if (
                text.StartsWith('第')
                && text.EndsWith('季')
                && int.TryParse(
                    text.AsSpan(1, text.Length - 2),
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out int season
                )
                && season > 0
            )
            {
                return season;
            }
        }

        return 0;
    }

    private static string DtoHeat(DramaDto data)
    {
        string score = ValueNormalizer.First(
            data.HotScoreData?.Score,
            data.HotScore,
            data.VideoDetail?.HotScoreData?.Score,
            data.VideoDetail?.HotScore
        );
        if (
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
        )
        {
            return score;
        }

        string explicitHeat = ValueNormalizer.First(
            data.HotScoreData?.Text,
            data.VideoDetail?.HotScoreData?.Text,
            data.RecText,
            data.VideoDetail?.RecText
        );
        if (explicitHeat.Length > 0)
        {
            return explicitHeat;
        }

        return ExtractHeatFromRecTags(data.RecTags)
            is { Length: > 0 } outerHeat
            ? outerHeat
            : ExtractHeatFromRecTags(data.VideoDetail?.RecTags);
    }

    private static string ExtractHeatFromRecTags(IReadOnlyList<DramaRecTagDto>? recTags)
    {
        if (recTags is null)
        {
            return string.Empty;
        }

        foreach (DramaRecTagDto tag in recTags)
        {
            if (string.IsNullOrWhiteSpace(tag?.Content))
            {
                continue;
            }

            string content = tag.Content.Trim();
            if (
                content.Contains("热度", StringComparison.Ordinal)
                || content.Contains("热搜", StringComparison.Ordinal)
            )
            {
                return content;
            }
        }

        return string.Empty;
    }

    private static string DtoOnlineDate(DramaDto data, DateTimeOffset now)
    {
        string timestamp = ValueNormalizer.First(
            data.FirstVisibleTime,
            data.VideoDetail?.FirstVisibleTime,
            data.CreateTime,
            data.VideoDetail?.CreateTime
        );
        if (
            long.TryParse(
                timestamp,
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

        foreach (
            DramaSubtitleDto item in (data.Subtitles ?? []).Concat(data.VideoDetail?.Subtitles ?? [])
        )
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
