using PhoenixReadApi.Internal.Contracts.App;
using PhoenixReadApi.Internal.Contracts.App.Comment;
using PhoenixReadApi.Internal.Contracts.App.Follow;
using PhoenixReadApi.Internal.Contracts.App.User;
using PhoenixReadApi.Internal.Features.Shared;
using PhoenixReadApi.Internal.Infrastructure.Http;
using PhoenixReadApi.Internal.Infrastructure.Runtime;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Features.Follow;

using User = PhoenixReadApi.User;

/// <summary>账号关注关系、粉丝与关注列表服务。</summary>
internal sealed class FollowService : PhoenixFeatureService
{
    private const int MaxPageSize = 50;

    internal FollowService(PhoenixReadClientRuntime runtime)
        : base(runtime) { }

    internal async Task<PhoenixOperationResult> SetFollowAsync(
        string userId,
        bool active,
        CancellationToken cancellationToken
    )
    {
        if (!ValueNormalizer.IsNumericId(userId = (userId ?? string.Empty).Trim()))
        {
            throw new ArgumentException("账号 ID 无效。", nameof(userId));
        }

        AppResponse<AppOperationResponse> response = await _appTransport
            .PostAsync<AppFollowActionPayload, AppOperationResponse>(
                "/novel/articleapi/do_action/v1/",
                new AppFollowActionPayload
                {
                    ObjectId = userId,
                    ObjectType = 1,
                    ActionType = active ? 25 : 26,
                    BusinessParameter = new AppFollowBusinessParameter
                    {
                        SharkParameter = new AppSharkParameter(),
                    },
                },
                commentRequest: false,
                cancellationToken,
                retryMode: AppRequestRetryMode.NoRetry
            )
            .ConfigureAwait(false);

        return new PhoenixOperationResult
        {
            Succeeded = true,
            Message = response.Message ?? string.Empty,
        };
    }

    internal Task<UserRelationPage> GetFollowersAsync(
        string userId,
        string cursor,
        int count,
        CancellationToken cancellationToken
    ) => LoadRelationListAsync(userId, 1508, cursor, count, cancellationToken);

    internal Task<UserRelationPage> GetFollowingsAsync(
        string userId,
        string cursor,
        int count,
        CancellationToken cancellationToken
    ) => LoadRelationListAsync(userId, 1507, cursor, count, cancellationToken);

    private async Task<UserRelationPage> LoadRelationListAsync(
        string userId,
        int commentSource,
        string cursor,
        int count,
        CancellationToken cancellationToken
    )
    {
        userId = NormalizeUserId(userId);
        ValidateCount(count);
        cursor = NormalizeCursor(cursor);

        AppUserRelationListPayload payload = new()
        {
            Count = count,
            CommentSource = commentSource,
            TargetUserId = userId,
            Cursor = cursor.Length > 0 ? cursor : null,
        };

        AppResponse<AppUserRelationListData> response = await _appTransport
            .PostAsync<AppUserRelationListPayload, AppUserRelationListData>(
                "/novel/commentapi/relation/list/" + userId + "/v1",
                payload,
                commentRequest: true,
                cancellationToken: cancellationToken,
                commentSource: commentSource,
                serverChannel: null
            )
            .ConfigureAwait(false);

        AppUserRelationListData? data = response.Data;
        List<UserRelationItem> items = [];
        HashSet<string> seen = [with(StringComparer.Ordinal)];

        foreach (AppUserRelationUgcUser? ugc in data?.Users ?? [])
        {
            if (ugc is null)
            {
                continue;
            }

            string id = ValueNormalizer.First(ugc.UserId, ugc.BaseInfo?.UserId).Trim();
            if (!ValueNormalizer.IsNumericId(id) || !seen.Add(id))
            {
                continue;
            }

            AppUgcUserBaseInfo baseInfo = ugc.BaseInfo ?? new();
            AppUgcUserRelation relation = ugc.UserRelation ?? new();
            AppUgcUserTag tag = ugc.UserTag ?? new();

            IReadOnlyList<string> roles =
            [
                .. (tag.SubTitleList ?? [])
                    .Where(static s => s is not null && !string.IsNullOrWhiteSpace(s.Text))
                    .Select(static s => s.Text.Trim())
                    .Distinct(StringComparer.Ordinal),
            ];

            bool isCreator = (tag.CreatorTypesFull?.Count ?? 0) > 0 || roles.Count > 0;

            items.Add(
                new UserRelationItem
                {
                    User = new User
                    {
                        Id = id,
                        Name = ValueNormalizer.First(baseInfo.UserName, id),
                        Introduction = baseInfo.Description ?? string.Empty,
                        AvatarUrl = baseInfo.UserAvatar,
                    },
                    Gender = baseInfo.Gender,
                    IsVerified = tag.IsBlueVip,
                    IsCreator = isCreator,
                    CreatorRoles = roles,
                    CanFollow = relation.CanFollow,
                    IsFollowing = relation.RelationType is 2 or 4,
                }
            );
        }

        return new UserRelationPage
        {
            Items = items,
            Cursor = data?.ListInfo?.Cursor ?? string.Empty,
            HasMore = data?.ListInfo?.HasMore ?? false,
            IsPrivacyProtected = data?.IsPrivacyOn ?? false,
        };
    }

    private static void ValidateCount(int count)
    {
        if (count is < 1 or > MaxPageSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(count),
                $"数量必须在 1 到 {MaxPageSize} 之间。"
            );
        }
    }

    private static string NormalizeCursor(string value)
    {
        value = (value ?? string.Empty).Trim();
        if (value.Length > 8192 || value.Any(static c => c is '\r' or '\n' or '\0'))
        {
            throw new ArgumentException("游标无效。", nameof(value));
        }
        return value;
    }
}
