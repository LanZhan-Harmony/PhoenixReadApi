using PhoenixReadApi.Internal.Contracts.App;
using PhoenixReadApi.Internal.Contracts.App.Comment;
using PhoenixReadApi.Internal.Contracts.App.Library;
using PhoenixReadApi.Internal.Infrastructure.Http;
using PhoenixReadApi.Internal.Infrastructure.Runtime;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Features.Library;

/// <summary>账号关注关系和关注动态。</summary>
internal sealed class FollowsService : LibraryServiceBase
{
    internal FollowsService(PhoenixReadClientRuntime runtime)
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
        return Succeeded(response.Message);
    }
}
