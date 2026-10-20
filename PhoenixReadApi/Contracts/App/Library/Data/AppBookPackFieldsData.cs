using System.Text.Json.Serialization;

namespace PhoenixReadApi.Internal.Contracts.App.Library;

/// <summary>书架字段接口返回的动态键值。</summary>
[JsonConverter(typeof(AppBookPackFieldsDataConverter))]
internal sealed class AppBookPackFieldsData
{
    public IReadOnlyDictionary<string, string> Values { get; init; } =
        new Dictionary<string, string>(StringComparer.Ordinal);
}
