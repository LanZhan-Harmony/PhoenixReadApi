using System.Globalization;
using PhoenixReadApi.Internal.Contracts.App.Catalog;
using PhoenixReadApi.Internal.Features.Shared;
using PhoenixReadApi.Internal.Infrastructure.Runtime;

namespace PhoenixReadApi.Internal.Features.Catalog;

/// <summary>负责原著小说与书籍分类落地页服务。</summary>
internal sealed class NovelCategoryService : PhoenixFeatureService
{
    internal NovelCategoryService(PhoenixReadClientRuntime runtime)
        : base(runtime) { }

    internal async Task<NovelCategoryPage> GetCategoryFrontAsync(
        string categoryId,
        int offset,
        int count,
        CancellationToken cancellationToken
    )
    {
        categoryId = (categoryId ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(categoryId))
        {
            throw new ArgumentException("分类 ID 不能为空。", nameof(categoryId));
        }

        AppNovelCategoryFrontResponse response = await _appTransport
            .GetRootAsync<AppNovelCategoryFrontResponse>(
                "/reading/bookapi/new_category/front/v1/",
                [
                    new("category_id", categoryId),
                    new("offset", Math.Max(0, offset).ToString(CultureInfo.InvariantCulture)),
                    new("count", Math.Clamp(count, 1, 50).ToString(CultureInfo.InvariantCulture)),
                    new("user_is_login", "0"),
                ],
                cancellationToken
            )
            .ConfigureAwait(false);

        AppNovelCategoryFrontData? data = response.Data;
        List<Novel> books = [];
        foreach (AppNovelBookDto dto in data?.BookList ?? [])
        {
            if (string.IsNullOrWhiteSpace(dto.BookId))
            {
                continue;
            }

            IReadOnlyList<string> tags = string.IsNullOrWhiteSpace(dto.Tags)
                ? []
                :
                [
                    .. dto.Tags.Split(
                        [',', '，', ' '],
                        StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
                    ),
                ];

            NovelCreationStatus status = dto.CreationStatus switch
            {
                "1" => NovelCreationStatus.Ongoing,
                "2" => NovelCreationStatus.Completed,
                _ => NovelCreationStatus.Unknown,
            };

            string statusText = dto.CreationStatus switch
            {
                "1" => "连载中",
                "2" => "已完结",
                _ => string.Empty,
            };

            books.Add(
                new Novel
                {
                    Id = dto.BookId,
                    Title = dto.BookName,
                    Author = dto.Author,
                    Description = dto.Description,
                    CoverUrl = dto.Cover,
                    Status = status,
                    StatusText = statusText,
                    ReadCount = dto.ReadCount,
                    Tags = tags,
                }
            );
        }

        return new NovelCategoryPage
        {
            CategoryId = data?.CategoryId ?? categoryId,
            CategoryName = data?.CategoryName ?? string.Empty,
            Books = books,
            HasMore = data?.HasMore ?? false,
        };
    }
}
