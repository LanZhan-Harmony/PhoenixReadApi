namespace PhoenixReadApi;

/// <summary>服务端操作结果。</summary>
public sealed class PhoenixOperationResult
{
    /// <summary>服务端是否报告成功。</summary>
    public bool Succeeded { get; init; }

    /// <summary>服务端返回的消息。</summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>操作创建的对象 ID（如评论 ID、回复 ID），若无则为空字符串。</summary>
    public string Id { get; init; } = string.Empty;
}
