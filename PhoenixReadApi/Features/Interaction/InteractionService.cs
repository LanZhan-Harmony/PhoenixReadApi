using PhoenixReadApi.Internal.Contracts.App;
using PhoenixReadApi.Internal.Contracts.App.Interaction;
using PhoenixReadApi.Internal.Features.Shared;
using PhoenixReadApi.Internal.Infrastructure.Http;
using PhoenixReadApi.Internal.Infrastructure.Runtime;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Features.Interaction;

/// <summary>不感兴趣与负反馈过滤功能服务。</summary>
internal sealed class InteractionService : PhoenixFeatureService
{
    internal InteractionService(PhoenixReadClientRuntime runtime)
        : base(runtime) { }

    internal async Task<DislikePanel> GetDislikePanelAsync(
        int cardType,
        CancellationToken cancellationToken
    )
    {
        AppResponse<AppCommonAbResultData> response = await _appTransport
            .GetAsync<AppCommonAbResultData>(
                "/reading/bookapi/common_ab_result/v1/",
                [],
                cancellationToken
            )
            .ConfigureAwait(false);

        AppCommonAbResultData? data = response.Data;
        IReadOnlyList<AppDislikeCardDto> cards = data?.PlayletCards ?? [];

        AppDislikeCardDto? matchedCard = cards.FirstOrDefault(c => c.CardType == cardType)
            ?? cards.FirstOrDefault();

        if (matchedCard is null)
        {
            return new DislikePanel { CardType = cardType, Groups = [] };
        }

        List<DislikeGroup> groups = [];
        foreach (AppDislikeGroupDto? groupDto in matchedCard.Groups ?? [])
        {
            if (groupDto is null)
            {
                continue;
            }

            List<DislikeOption> options = [];
            foreach (AppDislikeSelectionDto? sel in groupDto.Selections ?? [])
            {
                if (sel is null)
                {
                    continue;
                }

                options.Add(
                    new DislikeOption
                    {
                        Id = sel.Id,
                        Text = sel.Text,
                        Head = sel.Head,
                    }
                );
            }

            groups.Add(
                new DislikeGroup
                {
                    Title = groupDto.Title,
                    IconUrl = groupDto.IconUrl,
                    Options = options,
                }
            );
        }

        return new DislikePanel
        {
            CardType = matchedCard.CardType,
            Groups = groups,
        };
    }

    internal async Task<bool> SubmitDislikeAsync(
        string dramaId,
        string videoId,
        string recommendInfo,
        DislikeOption option,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(option);
        dramaId = NormalizeDramaId(dramaId);

        AppDislikePayload payload = new()
        {
            ActionCategory = 520,
            ActionType = 1,
            ObjectId = dramaId,
            ObjectType = 1,
            VideoId = videoId ?? string.Empty,
            SelectionId = option.Id,
            ActionReason = ValueNormalizer.First(option.Head, option.Text),
            RecommendInfo = recommendInfo ?? string.Empty,
        };

        AppResponse<AppOperationResponse> response = await _appTransport
            .PostAsync<AppDislikePayload, AppOperationResponse>(
                "/reading/articleapi/relation/update/v1/",
                payload,
                commentRequest: false,
                cancellationToken: cancellationToken,
                commentSource: 1506,
                serverChannel: null,
                retryMode: AppRequestRetryMode.NoRetry
            )
            .ConfigureAwait(false);

        return response.Code is "0" or "";
    }

    private static string NormalizeDramaId(string dramaId)
    {
        if (string.IsNullOrWhiteSpace(dramaId))
        {
            throw new ArgumentException("剧集 ID 不能为空。", nameof(dramaId));
        }

        string trimmed = dramaId.Trim();
        if (!ValueNormalizer.IsNumericId(trimmed))
        {
            throw new ArgumentException("剧集 ID 必须是由数字构成的字符串。", nameof(dramaId));
        }

        return trimmed;
    }
}
