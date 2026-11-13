namespace PhoenixReadApi;

/// <summary>登录支持的国家或地区区号配置。</summary>
public sealed class LoginRegion
{
    /// <summary>国家或地区中文名称（例如“中国大陆”、“中国香港”）。</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>国际区号代码（例如“86”、“852”）。</summary>
    public string Code { get; init; } = string.Empty;

    /// <summary>ISO 3166 国家/地区代码（例如“CN”、“HK”）。</summary>
    public string Iso { get; init; } = string.Empty;

    /// <summary>ISO 3166 国家/地区代码兼容别名（例如“CN”、“HK”）。</summary>
    public string IsoCode => Iso;

    /// <summary>手机号最小位数。</summary>
    public int MinLength { get; init; }

    /// <summary>手机号最大位数。</summary>
    public int MaxLength { get; init; }
}