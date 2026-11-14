using System.Net.Http.Headers;
using PhoenixReadApi.Internal.Contracts.App;
using PhoenixReadApi.Internal.Contracts.App.Comment;
using PhoenixReadApi.Internal.Contracts.App.Comment.Emoticon;
using PhoenixReadApi.Internal.Features.Shared;
using PhoenixReadApi.Internal.Infrastructure.Http;
using PhoenixReadApi.Internal.Infrastructure.Runtime;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Features.Comment;

/// <summary>表情包与评论图片上传服务。</summary>
internal sealed class EmoticonService : PhoenixFeatureService
{
    private const int DefaultCount = 20;
    private const int MaxCount = 50;

    internal EmoticonService(PhoenixReadClientRuntime runtime)
        : base(runtime) { }

    internal async Task<EmoticonPage> GetEmoticonsAsync(
        bool isCollected,
        int offset,
        int count,
        CancellationToken cancellationToken
    )
    {
        ValidateCount(count);
        ValidateOffset(offset);

        AppResponse<AppEmoticonData> response = await _appTransport
            .GetAsync<AppEmoticonData>(
                "/reading/ugc/comment/emoticon/v1/",
                [
                    new("source_type", "1"),
                    new("search_type", isCollected ? "4" : "2"),
                    new("count", count.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                    new("offset", offset.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                ],
                cancellationToken
            )
            .ConfigureAwait(false);

        AppEmoticonData? data = response.Data;
        List<CommentImage> items = [];
        HashSet<string> seen = new(StringComparer.Ordinal);

        foreach (AppEmoticonImage? img in data?.Items ?? [])
        {
            if (img is null)
            {
                continue;
            }

            string url = ValueNormalizer.First(img.WebUrl, img.ExpandWebUrl, img.DynamicUrl);
            string id = img.Id.Trim();
            string key = id.Length > 0 ? id : url;
            if (string.IsNullOrWhiteSpace(url) || !seen.Add(key))
            {
                continue;
            }

            items.Add(
                new CommentImage
                {
                    Id = id,
                    Url = url,
                    WebUri = img.WebUri,
                    Width = Math.Max(0, img.Width),
                    Height = Math.Max(0, img.Height),
                    ImageType = img.ImageType,
                }
            );
        }

        return new EmoticonPage
        {
            Items = items,
            NextOffset = data?.NextOffset ?? offset,
            HasMore = data?.HasMore ?? false,
        };
    }

    internal async Task<bool> CollectEmoticonAsync(
        string imageId,
        bool isAdd,
        string imageUrl,
        int imageWidth,
        int imageHeight,
        int imageType,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(imageId) && string.IsNullOrWhiteSpace(imageUrl))
        {
            throw new ArgumentException("表情 ID 与表情 URL 不能同时为空。", nameof(imageId));
        }

        AppCollectEmoticonPayload payload = new()
        {
            CollectType = isAdd ? 1 : 2,
            ImageId = imageId ?? string.Empty,
            ImageUrl = imageUrl ?? string.Empty,
            ImageWidth = Math.Max(0, imageWidth),
            ImageHeight = Math.Max(0, imageHeight),
            ImageType = imageType,
        };

        AppResponse<AppOperationResponse> response = await _appTransport
            .PostAsync<AppCollectEmoticonPayload, AppOperationResponse>(
                "/reading/ugc/comment/emoticon/collect/v1/",
                payload,
                commentRequest: true,
                cancellationToken: cancellationToken,
                commentSource: 1506,
                serverChannel: null,
                retryMode: AppRequestRetryMode.NoRetry
            )
            .ConfigureAwait(false);

        return response.Code is "0" or "";
    }

    internal async Task<UploadedImage> UploadPictureAsync(
        Stream imageStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(imageStream);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            fileName = "image.png";
        }
        if (string.IsNullOrWhiteSpace(contentType))
        {
            contentType = "application/octet-stream";
        }

        using MultipartFormDataContent multipart = [];
        using StreamContent streamContent = new(imageStream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        multipart.Add(streamContent, "picture", fileName);

        AppResponse<AppUploadPictureData> response = await _appTransport
            .PostMultipartAsync<AppUploadPictureData>(
                "/reading/reader/upload/picture/",
                multipart,
                cancellationToken
            )
            .ConfigureAwait(false);

        AppUploadPictureData data = response.Data
            ?? throw new PhoenixReadApiResponseException("图片上传未返回有效数据。", "upload-picture");

        return new UploadedImage
        {
            ImageId = data.ImageId,
            Url = data.WebUrl,
            WebUri = data.WebUri,
            Width = data.Width,
            Height = data.Height,
        };
    }

    internal async Task<UploadedImage> UploadPictureAsync(
        byte[] imageBytes,
        string fileName,
        string contentType,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(imageBytes);
        using MemoryStream ms = new(imageBytes, writable: false);
        return await UploadPictureAsync(ms, fileName, contentType, cancellationToken)
            .ConfigureAwait(false);
    }

    private static void ValidateCount(int count)
    {
        if (count is < 1 or > MaxCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(count),
                count,
                "分页数量必须在 1 到 " + MaxCount + " 之间。"
            );
        }
    }

    private static void ValidateOffset(int offset)
    {
        if (offset < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(offset),
                offset,
                "分页偏移量不能为负数。"
            );
        }
    }
}
