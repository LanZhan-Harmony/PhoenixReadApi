using System.Globalization;
using System.Text.RegularExpressions;

namespace PhoenixReadApi.Internal.Infrastructure.Web;

/// <summary>Normalizes provider date labels and timestamps.</summary>
internal static partial class DateNormalizer
{
    internal static string Normalize(string value)
    {
        Match match = DateRegex().Match(value);
        if (
            !match.Success
            || !int.TryParse(match.Groups[1].Value, out var year)
            || !int.TryParse(match.Groups[2].Value, out var month)
            || !int.TryParse(match.Groups[3].Value, out var day)
            || year is < 2000 or > 2100
        )
        {
            return string.Empty;
        }

        return DateTime.TryParseExact(
            $"{year:0000}-{month:00}-{day:00}",
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out DateTime date
        )
            ? date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            : string.Empty;
    }

    [GeneratedRegex(@"(\d{4})[年\-/](\d{1,2})[月\-/](\d{1,2})")]
    private static partial Regex DateRegex();
}
