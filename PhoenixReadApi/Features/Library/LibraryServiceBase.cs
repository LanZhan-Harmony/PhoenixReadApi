using PhoenixReadApi.Internal.Features.Shared;
using PhoenixReadApi.Internal.Infrastructure.Runtime;

namespace PhoenixReadApi.Internal.Features.Library;

/// <summary>个人库服务共用的运行时依赖、校验和响应辅助基类。</summary>
internal abstract class LibraryServiceBase(PhoenixReadClientRuntime runtime)
    : PhoenixFeatureService(runtime)
{
    protected const int MaxPageSize = 50;

    protected static PhoenixOperationResult Succeeded(string message) =>
        new() { Succeeded = true, Message = message ?? string.Empty };

    protected static void ValidatePage(int count, int offset)
    {
        ValidateCount(count);
        if (offset is < 0 or > 1_000_000)
        {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }
    }

    protected static void ValidateCount(int count)
    {
        if (count is < 1 or > MaxPageSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(count),
                $"数量必须在 1 到 {MaxPageSize} 之间。"
            );
        }
    }

    protected static string NormalizeCursor(string value)
    {
        value = (value ?? string.Empty).Trim();
        if (value.Length > 8192 || value.Any(static c => c is '\r' or '\n' or '\0'))
        {
            throw new ArgumentException("游标无效。", nameof(value));
        }
        return value;
    }

    protected static Dictionary<string, string> ParseQuery(string query)
    {
        Dictionary<string, string> values = new(StringComparer.OrdinalIgnoreCase);
        foreach (
            string item in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries)
        )
        {
            string[] parts = item.Split('=', 2);
            if (parts.Length == 2)
            {
                values[Uri.UnescapeDataString(parts[0])] = Uri.UnescapeDataString(parts[1]);
            }
        }
        return values;
    }

    protected static string FirstQuery(
        IReadOnlyDictionary<string, string> values,
        params string[] names
    )
    {
        foreach (string name in names)
        {
            if (values.TryGetValue(name, out string? value) && value.Length > 0)
            {
                return value;
            }
        }
        return string.Empty;
    }
}
