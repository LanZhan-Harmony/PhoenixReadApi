using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;

namespace PhoenixReadApi.Internal.Infrastructure.Crypto;

internal static class DeviceIdGenerator
{
    internal static string Create()
    {
        Span<byte> span = stackalloc byte[8];
        RandomNumberGenerator.Fill(span);
        return (
            1000000000000000000L + BinaryPrimitives.ReadUInt64BigEndian(span) % 8000000000000000000L
        ).ToString(CultureInfo.InvariantCulture);
    }

    internal static bool IsValid(string? value)
    {
        if (value is null or { Length: 0 or > 32 })
        {
            return false;
        }

        bool hasNonZero = false;
        foreach (char c in value)
        {
            if (c is < '0' or > '9')
            {
                return false;
            }

            if (c != '0')
            {
                hasNonZero = true;
            }
        }

        return hasNonZero;
    }
}
