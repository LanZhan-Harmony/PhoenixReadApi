using System.Text;
using System.Text.Json;
using PhoenixReadApi.Internal.Infrastructure.Http;
using PhoenixReadApi.Internal.Infrastructure.Runtime;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Features.Shared;

/// <summary>
/// 功能服务的通用辅助方法。此类不保存功能状态，也不负责转发公共 API
/// 只提供统一的运行时依赖和共享校验规则。
/// </summary>
internal abstract class PhoenixFeatureService(PhoenixReadClientRuntime runtime)
{
    protected PhoenixReadClientRuntime Runtime { get; } = runtime;

    protected PhoenixReadApiOptions _options => Runtime.Options;

    protected PhoenixReadAppTransport _appTransport => Runtime.AppTransport;

    protected PhoenixReadWebTransport _webTransport => Runtime.WebTransport;

    protected Task<string> GetTextAsync(
        Uri endpoint,
        Uri refererBase,
        string logicalEndpoint,
        CancellationToken cancellationToken,
        int? maxBytes = null
    ) => Runtime.GetTextAsync(endpoint, refererBase, logicalEndpoint, cancellationToken, maxBytes);

    protected static (string Genre, string Scene, string Category) CategoryInfo(
        PhoenixReadApiCategory category
    ) =>
        category switch
        {
            PhoenixReadApiCategory.RealDrama => ("short_play", "default", "真人剧"),
            PhoenixReadApiCategory.ComicDrama => ("comic_series", "comic_series", "漫剧"),
            PhoenixReadApiCategory.AiDrama => ("ai_series", "ai_series", "AI剧"),
            _ => throw new ArgumentException("该分类不支持 App 推荐接口。", nameof(category)),
        };

    protected static (string Genre, string Scene, string Category) CategoryInfo(string genre) =>
        genre switch
        {
            "short_play" => ("short_play", "default", "真人剧"),
            "comic_series" => ("comic_series", "comic_series", "漫剧"),
            "ai_series" => ("ai_series", "ai_series", "AI剧"),
            _ => throw new ArgumentException("推荐分类无效。", nameof(genre)),
        };

    protected static void ValidateCategory(PhoenixReadApiCategory category)
    {
        if (!Enum.IsDefined(category))
        {
            throw new ArgumentOutOfRangeException(nameof(category));
        }
    }

    protected static void ValidateRecommendationQuery(RecommendationQuery query)
    {
        _ = CategoryInfo(query.Genre);
        if (
            query.Offset is < 0 or > 1_000_000
            || query.SessionId.Length > 4096
            || query.SessionId.Any(static c => c is '\r' or '\n' or '\0')
            || query.Seen.Count > 540
        )
        {
            throw new ArgumentException("推荐分类或分页参数无效。", nameof(query));
        }

        foreach (var id in query.Seen)
        {
            var value = id.StartsWith("hongguo:", StringComparison.Ordinal) ? id[8..] : id;
            if (!JsonValueReader.IsNumericId(value))
            {
                throw new ArgumentException("推荐分页含无效剧集 ID。", nameof(query));
            }
        }
    }

    protected static string NormalizeSeriesId(string value)
    {
        value = (value ?? string.Empty).Trim();
        if (value.StartsWith("hongguo:", StringComparison.Ordinal))
        {
            value = value[8..];
        }

        if (!JsonValueReader.IsNumericId(value))
        {
            throw new ArgumentException("红果剧集 ID 无效。", nameof(value));
        }

        return value;
    }

    protected static string NormalizeVideoId(string value)
    {
        value = (value ?? string.Empty).Trim();
        if (value.StartsWith("hongguo-cenc://", StringComparison.Ordinal))
        {
            value = value[15..];
        }

        if (!JsonValueReader.IsNumericId(value))
        {
            throw new ArgumentException("红果视频 ID 无效。", nameof(value));
        }

        return value;
    }

    protected static string NormalizeKeyword(string value)
    {
        value = (value ?? string.Empty).Normalize(NormalizationForm.FormKC).Trim();
        if (value.Length == 0 || value.EnumerateRunes().Count() > 80 || value.Any(char.IsControl))
        {
            throw new ArgumentException("请输入 1 至 80 个字符的搜索词。", nameof(value));
        }

        return value;
    }

    protected static bool IsRecoverable(Exception exception) =>
        exception
            is HttpRequestException
                or TimeoutException
                or PhoenixReadApiTransportException
                or PhoenixReadApiHttpException
                or PhoenixReadApiProviderException
                or PhoenixReadApiDeserializationException
                or PhoenixReadApiResponseException
                or PhoenixReadApiMediaException
                or PhoenixReadApiFallbackException;

    protected static string PublicError(Exception? exception) =>
        exception switch
        {
            null => "未执行",
            PhoenixReadApiException api => api.Message,
            OperationCanceledException => "请求已取消",
            TimeoutException => "请求超时",
            _ => string.IsNullOrWhiteSpace(exception.Message) ? "请求失败" : exception.Message,
        };

    protected static PhoenixReadApiException ToApiException(
        string message,
        string endpoint,
        Exception? exception
    )
    {
        if (exception is PhoenixReadApiException api)
        {
            return api;
        }

        return exception switch
        {
            HttpRequestException => new PhoenixReadApiTransportException(
                message,
                endpoint,
                innerException: exception
            ),
            TimeoutException => new PhoenixReadApiTransportException(
                message,
                endpoint,
                innerException: exception
            ),
            JsonException => new PhoenixReadApiDeserializationException(
                message,
                endpoint,
                exception
            ),
            _ => new PhoenixReadApiException(message, endpoint, null, exception),
        };
    }

    protected static List<Drama> MergeDramas(
        IEnumerable<Drama> original,
        IEnumerable<Drama> additions
    )
    {
        List<Drama> output = [];
        Dictionary<string, int> positions = new(StringComparer.Ordinal);
        foreach (Drama? drama in original.Concat(additions))
        {
            if (drama.Id.Length == 0)
            {
                continue;
            }

            if (positions.TryGetValue(drama.Id, out var position))
            {
                output[position] = MergeDrama(output[position], drama);
            }
            else
            {
                positions[drama.Id] = output.Count;
                output.Add(drama);
            }
        }

        return output;
    }

    private static Drama MergeDrama(Drama first, Drama second) =>
        new()
        {
            Id = first.Id,
            Title = JsonValueReader.First(first.Title, second.Title),
            Description = JsonValueReader.First(first.Description, second.Description),
            CoverUrl = JsonValueReader.First(first.CoverUrl, second.CoverUrl),
            Category = JsonValueReader.First(first.Category, second.Category),
            Remark = JsonValueReader.First(first.Remark, second.Remark),
            Score = JsonValueReader.First(first.Score, second.Score),
            Views = JsonValueReader.First(first.Views, second.Views),
            Heat = JsonValueReader.First(first.Heat, second.Heat),
            OnlineDate = JsonValueReader.First(first.OnlineDate, second.OnlineDate),
            EpisodeCount = Math.Max(first.EpisodeCount, second.EpisodeCount),
            ReleaseStatus = JsonValueReader.First(first.ReleaseStatus, second.ReleaseStatus),
            Tags =
            [
                .. first
                    .Tags.Concat(second.Tags)
                    .Where(static tag => !string.IsNullOrWhiteSpace(tag))
                    .Distinct(StringComparer.Ordinal),
            ],
        };
}
