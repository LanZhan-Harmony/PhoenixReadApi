using System.Globalization;
using System.Text.RegularExpressions;

namespace PhoenixReadApi.Internal.Infrastructure.Web;

/// <summary>规范化红果接口返回的日期标签和时间戳。</summary>
internal static partial class DateNormalizer
{
    internal static string Normalize(string value)
    {
        Match match = DateRegex().Match(value);
        if (
            !match.Success
            || !int.TryParse(match.Groups[1].ValueSpan, CultureInfo.InvariantCulture, out int year)
            || !int.TryParse(match.Groups[2].ValueSpan, CultureInfo.InvariantCulture, out int month)
            || !int.TryParse(match.Groups[3].ValueSpan, CultureInfo.InvariantCulture, out int day)
            || year is < 2000 or > 2100
            || month is < 1 or > 12
            || day < 1
            || day > DateTime.DaysInMonth(year, month)
        )
        {
            return string.Empty;
        }

        return string.Create(
            CultureInfo.InvariantCulture,
            stackalloc char[10],
            $"{year:0000}-{month:00}-{day:00}"
        );
    }

    [GeneratedRegex(@"(\d{4})[年\-/](\d{1,2})[月\-/](\d{1,2})")]
    private static partial Regex DateRegex();
}
