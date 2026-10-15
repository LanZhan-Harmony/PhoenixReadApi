using System.Text.Json;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Infrastructure.Web;

/// <summary>Locates dynamic loader keys without exposing route naming to feature code.</summary>
internal static class RouterDataLocator
{
    internal static JsonElement Find(JsonDocument? document, params string[] names)
    {
        if (
            document is null
            || document.RootElement.ValueKind != JsonValueKind.Object
            || !document.RootElement.TryGetProperty("loaderData", out JsonElement loader)
            || loader.ValueKind != JsonValueKind.Object
        )
        {
            return default;
        }

        foreach (var name in names)
        {
            if (
                loader.TryGetProperty(name, out JsonElement exact)
                && exact.ValueKind == JsonValueKind.Object
            )
            {
                return exact;
            }
        }

        foreach (JsonProperty item in loader.EnumerateObject())
        {
            foreach (var name in names)
            {
                var prefix = name.TrimEnd('$');
                if (
                    prefix.Length > 0
                    && item.Name.StartsWith(prefix, StringComparison.Ordinal)
                    && item.Value.ValueKind == JsonValueKind.Object
                )
                {
                    return item.Value;
                }
            }
        }

        return default;
    }

    internal static T? Deserialize<T>(JsonDocument? document, params string[] names)
    {
        JsonElement section = Find(document, names);
        return section.ValueKind == JsonValueKind.Object
            ? JsonAotSerializer.Deserialize<T>(section.GetRawText())
            : default;
    }
}
