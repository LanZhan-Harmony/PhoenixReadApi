using PhoenixReadApi.Internal.Contracts.App;
using PhoenixReadApi.Internal.Contracts.App.Comment;
using PhoenixReadApi.Internal.Features.Shared;
using PhoenixReadApi.Internal.Infrastructure.Runtime;
using PhoenixReadApi.Internal.Serialization;

namespace PhoenixReadApi.Internal.Features.Actors;

/// <summary>加载演员主页及其参演剧集。</summary>
internal sealed class ActorService : PhoenixFeatureService
{
    internal ActorService(PhoenixReadClientRuntime runtime)
        : base(runtime) { }

    internal Task<Actor> GetAsync(string profileId, CancellationToken cancellationToken)
    {
        profileId = NormalizeActorId(profileId);
        return LoadAsync(
            new DramaActor
            {
                Id = profileId,
                ProfileId = profileId,
            },
            cancellationToken
        );
    }

    internal Task<Actor> GetAsync(DramaActor actor, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(actor);
        var profileId = NormalizeActorId(
            !string.IsNullOrWhiteSpace(actor.ProfileId) ? actor.ProfileId : actor.Id
        );
        return LoadAsync(
            new DramaActor
            {
                Id = actor.Id ?? string.Empty,
                ProfileId = profileId,
                Name = actor.Name ?? string.Empty,
                Introduction = actor.Introduction ?? string.Empty,
                AvatarUrl = actor.AvatarUrl ?? string.Empty,
                RoleName = actor.RoleName ?? string.Empty,
            },
            cancellationToken
        );
    }

    private async Task<Actor> LoadAsync(DramaActor actor, CancellationToken cancellationToken)
    {
        AppActorProfilePayload payload = new() { TargetUserId = actor.ProfileId };
        AppResponse<AppActorProfileData> response = await _appTransport
            .PostAsync<AppActorProfilePayload, AppActorProfileData>(
                "/novel/commentapi/user/profile/" + actor.ProfileId + "/v1",
                payload,
                commentRequest: true,
                cancellationToken,
                commentSource: 1506,
                serverChannel: null
            )
            .ConfigureAwait(false);
        AppActorProfileData data =
            response.Data
            ?? throw new PhoenixReadApiResponseException("演员主页未返回有效数据。", "actor");

        List<Drama> works = [];
        HashSet<string> seen = new(StringComparer.Ordinal);
        foreach (AppActorWorkRecord? record in data.Items ?? [])
        {
            if (record is null || record.Video is null)
            {
                continue;
            }

            AppActorVideo video = record.Video;
            var id = (video.Id ?? string.Empty).Trim();
            if (!JsonValueReader.IsNumericId(id) || !seen.Add(id))
            {
                continue;
            }

            AppActorVideoCommon common = video.Common ?? new();
            AppActorVideoExpand expand = video.Expand ?? new();

            works.Add(
                DramaMapper.FromActorWork(
                    id,
                    common.Title ?? string.Empty,
                    common.Introduction ?? string.Empty,
                    common.CoverUrl ?? string.Empty,
                    expand.EpisodeCount
                )
            );
        }

        return new Actor
        {
            Id = !string.IsNullOrWhiteSpace(actor.Id) ? actor.Id : actor.ProfileId,
            ProfileId = actor.ProfileId,
            Name = actor.Name ?? string.Empty,
            Introduction = actor.Introduction ?? string.Empty,
            AvatarUrl = actor.AvatarUrl ?? string.Empty,
            Works = works,
        };
    }

    private static string NormalizeActorId(string value)
    {
        value = (value ?? string.Empty).Trim();
        if (!JsonValueReader.IsNumericId(value))
        {
            throw new ArgumentException("演员 ID 无效。", nameof(value));
        }

        return value;
    }
}
