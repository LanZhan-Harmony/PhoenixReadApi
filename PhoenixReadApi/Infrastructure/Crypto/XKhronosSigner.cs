using System.Globalization;

namespace PhoenixReadApi.Internal.Infrastructure.Crypto;

/// <summary>
/// 六神算法之 X-Khronos 签名器：负责时间戳的统一生成与格式化。
/// </summary>
internal static class XKhronosSigner
{
    /// <summary>格式化秒级时间戳为 X-Khronos 字符串。</summary>
    public static string Format(uint timestamp) => timestamp.ToString(CultureInfo.InvariantCulture);

    /// <summary>格式化毫秒或秒级长整数为 X-Khronos 字符串。</summary>
    public static string Format(long timestamp) => timestamp.ToString(CultureInfo.InvariantCulture);
}
