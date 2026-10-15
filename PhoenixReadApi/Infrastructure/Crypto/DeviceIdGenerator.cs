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

    internal static bool IsValid(string? value) =>
        value is not null
        && value.Length is > 0 and <= 32
        && value.All(static c => c is >= '0' and <= '9');
}
