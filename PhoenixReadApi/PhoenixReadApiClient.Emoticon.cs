namespace PhoenixReadApi;

public sealed partial class PhoenixReadApiClient
{
    /// <summary>获取表情包列表的一页。</summary>
    /// <param name="isCollected">true 为查询当前账号收藏的表情，false 为查询系统表情包。</param>
    /// <param name="offset">分页偏移量，首次请求传 0，后续传上一页的 NextOffset。</param>
    /// <param name="count">每页数量，默认为 20，最大为 50。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>表情包图片分页结果。</returns>
    public Task<EmoticonPage> GetEmoticonsAsync(
        bool isCollected = false,
        int offset = 0,
        int count = 20,
        CancellationToken cancellationToken = default
    ) => _emoticon.GetEmoticonsAsync(isCollected, offset, count, cancellationToken);

    /// <summary>收藏或移除表情包。</summary>
    /// <param name="imageId">表情 ID。</param>
    /// <param name="isAdd">true 为收藏添加，false 为移除收藏。</param>
    /// <param name="imageUrl">表情 URL（添加时可传入）。</param>
    /// <param name="imageWidth">表情宽度（像素）。</param>
    /// <param name="imageHeight">表情高度（像素）。</param>
    /// <param name="imageType">表情类型编码。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>操作是否成功。</returns>
    public Task<bool> CollectEmoticonAsync(
        string imageId,
        bool isAdd,
        string imageUrl = "",
        int imageWidth = 0,
        int imageHeight = 0,
        int imageType = 0,
        CancellationToken cancellationToken = default
    ) =>
        _emoticon.CollectEmoticonAsync(
            imageId,
            isAdd,
            imageUrl,
            imageWidth,
            imageHeight,
            imageType,
            cancellationToken
        );

    /// <summary>上传评论配图或剧评图片（Stream 流式上传）。</summary>
    /// <param name="imageStream">包含图片二进制数据的流对象。</param>
    /// <param name="fileName">图片文件名，默认为 "image.png"。</param>
    /// <param name="contentType">图片 MIME 类型，默认为 "image/png"。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>上传成功后返回的图片元数据（包含站内 URI 和公网 URL）。</returns>
    public Task<UploadedImage> UploadPictureAsync(
        Stream imageStream,
        string fileName = "image.png",
        string contentType = "image/png",
        CancellationToken cancellationToken = default
    ) => _emoticon.UploadPictureAsync(imageStream, fileName, contentType, cancellationToken);

    /// <summary>上传评论配图或剧评图片（byte 数组上传）。</summary>
    /// <param name="imageBytes">图片字节数组。</param>
    /// <param name="fileName">图片文件名，默认为 "image.png"。</param>
    /// <param name="contentType">图片 MIME 类型，默认为 "image/png"。</param>
    /// <param name="cancellationToken">用于取消请求的令牌。</param>
    /// <returns>上传成功后返回的图片元数据（包含站内 URI 和公网 URL）。</returns>
    public Task<UploadedImage> UploadPictureAsync(
        byte[] imageBytes,
        string fileName = "image.png",
        string contentType = "image/png",
        CancellationToken cancellationToken = default
    ) => _emoticon.UploadPictureAsync(imageBytes, fileName, contentType, cancellationToken);
}
