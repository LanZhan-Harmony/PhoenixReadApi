using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using PhoenixReadApi.Internal.Contracts.App;
using PhoenixReadApi.Internal.Contracts.App.Authentication;
using PhoenixReadApi.Internal.Contracts.App.Catalog;
using PhoenixReadApi.Internal.Contracts.App.Comment;
using PhoenixReadApi.Internal.Contracts.App.Comment.Emoticon;
using PhoenixReadApi.Internal.Contracts.App.Danmaku;
using PhoenixReadApi.Internal.Contracts.App.Drama;
using PhoenixReadApi.Internal.Contracts.App.Interaction;
using PhoenixReadApi.Internal.Contracts.App.Library;
using PhoenixReadApi.Internal.Contracts.App.Notification;
using PhoenixReadApi.Internal.Contracts.App.Playback;
using PhoenixReadApi.Internal.Contracts.App.Search;
using PhoenixReadApi.Internal.Contracts.App.User;
using PhoenixReadApi.Internal.Contracts.Shared.Drama;
using PhoenixReadApi.Internal.Contracts.Web.Authentication;
using PhoenixReadApi.Internal.Contracts.Web.Catalog;
using PhoenixReadApi.Internal.Contracts.Web.Drama;
using PhoenixReadApi.Internal.Contracts.Web.Playback;
using PhoenixReadApi.Internal.Contracts.Web.Search;

namespace PhoenixReadApi.Internal.Serialization;

[JsonSourceGenerationOptions(
    GenerationMode = JsonSourceGenerationMode.Metadata,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
)]
// ============================================================================
// 1. App 业务数据传输对象与请求载荷 (App DTOs & Payloads, A-Z)
// ============================================================================
[JsonSerializable(typeof(AppAlbumData))]
[JsonSerializable(typeof(AppAlbumModel))]
[JsonSerializable(typeof(AppAlbumPayload))]
[JsonSerializable(typeof(AppAlbumSeasonItemDto))]
[JsonSerializable(typeof(AppArticleActionPayload))]
[JsonSerializable(typeof(AppBookmallCellChangeData))]
[JsonSerializable(typeof(AppBookmallCellChangeResponse))]
[JsonSerializable(typeof(AppBookmallCellDto))]
[JsonSerializable(typeof(AppBookmallCellViewDto))]
[JsonSerializable(typeof(AppBookmallTabData))]
[JsonSerializable(typeof(AppBookmallTabItemDto))]
[JsonSerializable(typeof(AppBookmallTabResponse))]
[JsonSerializable(typeof(AppBusinessParameter))]
[JsonSerializable(typeof(AppCatalogPayload))]
[JsonSerializable(typeof(AppCollectEmoticonPayload))]
[JsonSerializable(typeof(AppCommentActionPayload))]
[JsonSerializable(typeof(AppCommentAddCommentInfo))]
[JsonSerializable(typeof(AppCommentAddData))]
[JsonSerializable(typeof(AppCommentAddPayload))]
[JsonSerializable(typeof(AppCommentDeletePayload))]
[JsonSerializable(typeof(AppCommentImageDataDto))]
[JsonSerializable(typeof(AppCommentImageDataListDto))]
[JsonSerializable(typeof(AppCommentImageDto))]
[JsonSerializable(typeof(AppCommentListPayload))]
[JsonSerializable(typeof(AppCommentPayload))]
[JsonSerializable(typeof(AppCommonAbResultData))]
[JsonSerializable(typeof(AppCurrentUserInfoData))]
[JsonSerializable(typeof(AppDailyReleaseCalendarSchemaDto))]
[JsonSerializable(typeof(AppDailyReleaseData))]
[JsonSerializable(typeof(AppDailyReleaseItemDto))]
[JsonSerializable(typeof(AppDailyReleaseResponse))]
[JsonSerializable(typeof(AppDailyReleaseSubscribeDataDto))]
[JsonSerializable(typeof(AppDetailPayload))]
[JsonSerializable(typeof(AppDeviceRegisterPayload))]
[JsonSerializable(typeof(AppDeviceRegisterResponse))]
[JsonSerializable(typeof(AppDislikeCardDto))]
[JsonSerializable(typeof(AppDislikeGroupDto))]
[JsonSerializable(typeof(AppDislikePayload))]
[JsonSerializable(typeof(AppDislikeSelectionDto))]
[JsonSerializable(typeof(AppDynamicAudioDto))]
[JsonSerializable(typeof(AppDynamicVideoDto))]
[JsonSerializable(typeof(AppEmoticonData))]
[JsonSerializable(typeof(AppEmoticonImage))]
[JsonSerializable(typeof(AppEncryptionDto))]
[JsonSerializable(typeof(AppFollowActionPayload))]
[JsonSerializable(typeof(AppFollowUpdateActionPayload))]
[JsonSerializable(typeof(AppFollowUpdateCommon))]
[JsonSerializable(typeof(AppFollowUpdateExpand))]
[JsonSerializable(typeof(AppFollowUpdateExtra))]
[JsonSerializable(typeof(AppFollowUpdateInfo))]
[JsonSerializable(typeof(AppFollowUpdateListData))]
[JsonSerializable(typeof(AppFollowUpdateListInfo))]
[JsonSerializable(typeof(AppFollowUpdateRecord))]
[JsonSerializable(typeof(AppFollowUpdateVideo))]
[JsonSerializable(typeof(AppHotSearchLynxCellDto))]
[JsonSerializable(typeof(AppHotSearchPlanCellDto))]
[JsonSerializable(typeof(AppHotSearchPlanResponse))]
[JsonSerializable(typeof(AppHotSearchTagDto))]
[JsonSerializable(typeof(AppHotSearchTopicDto))]
[JsonSerializable(typeof(AppHotSearchTopicInfoDto))]
[JsonSerializable(typeof(AppHotSearchVideoDto))]
[JsonSerializable(typeof(AppMediaPayload))]
[JsonSerializable(typeof(AppMediaVariantDto))]
[JsonSerializable(typeof(AppMultiDetailItem))]
[JsonSerializable(typeof(AppMultiMediaPayload))]
[JsonSerializable(typeof(AppNovelBookDto))]
[JsonSerializable(typeof(AppNovelCategoryFrontData))]
[JsonSerializable(typeof(AppNovelCategoryFrontResponse))]
[JsonSerializable(typeof(AppPlanCellDto))]
[JsonSerializable(typeof(AppPlanResponse))]
[JsonSerializable(typeof(AppPlaybackModel))]
[JsonSerializable(typeof(AppPlayerBatchBizParam))]
[JsonSerializable(typeof(AppPlayerBatchDetailPayload))]
[JsonSerializable(typeof(AppPlayerSeriesEntry))]
[JsonSerializable(typeof(AppReadHistoryUpdatePayload))]
[JsonSerializable(typeof(AppReplyAddData))]
[JsonSerializable(typeof(AppReplyAddReplyInfo))]
[JsonSerializable(typeof(AppReplyBusinessParameter))]
[JsonSerializable(typeof(AppReplyCommon))]
[JsonSerializable(typeof(AppReplyListPayload))]
[JsonSerializable(typeof(AppReplyStat))]
[JsonSerializable(typeof(AppReplyUserBaseInfo))]
[JsonSerializable(typeof(AppReplyUserInfo))]
[JsonSerializable(typeof(AppReservationActionPayload))]
[JsonSerializable(typeof(AppSearchCueItemDto))]
[JsonSerializable(typeof(AppSearchCueResponse))]
[JsonSerializable(typeof(AppSearchTabCell))]
[JsonSerializable(typeof(AppSearchTabData))]
[JsonSerializable(typeof(AppSearchTabItem))]
[JsonSerializable(typeof(AppSearchTabResponse))]
[JsonSerializable(typeof(AppSearchUserBaseInfo))]
[JsonSerializable(typeof(AppSearchUserDto))]
[JsonSerializable(typeof(AppSessionSnapshot))]
[JsonSerializable(typeof(AppShelfUpdatePayload))]
[JsonSerializable(typeof(AppShortFilterData))]
[JsonSerializable(typeof(AppShortFilterItemDto))]
[JsonSerializable(typeof(AppShortFilterPayload))]
[JsonSerializable(typeof(AppShortFilterResponse))]
[JsonSerializable(typeof(AppShortFilterRowDto))]
[JsonSerializable(typeof(AppShortRankActorBaseInfoDto))]
[JsonSerializable(typeof(AppShortRankActorDto))]
[JsonSerializable(typeof(AppShortRankActorUserDto))]
[JsonSerializable(typeof(AppShortRankCellItemDto))]
[JsonSerializable(typeof(AppShortRankCellSelectorDto))]
[JsonSerializable(typeof(AppShortRankCellViewDto))]
[JsonSerializable(typeof(AppShortRankChangeDto))]
[JsonSerializable(typeof(AppShortRankDataContainer))]
[JsonSerializable(typeof(AppShortRankRecTextDto))]
[JsonSerializable(typeof(AppShortRankResponse))]
[JsonSerializable(typeof(AppShortRankSelectorItemDto))]
[JsonSerializable(typeof(AppShortRankSelectorRowDto))]
[JsonSerializable(typeof(AppShortRankSubtitleDto))]
[JsonSerializable(typeof(AppSubTitleItem))]
[JsonSerializable(typeof(AppTagDramaItem))]
[JsonSerializable(typeof(AppTagLandingData))]
[JsonSerializable(typeof(AppTagSelectorContainer))]
[JsonSerializable(typeof(AppTagSelectorItemDto))]
[JsonSerializable(typeof(AppTagSelectorRowDto))]
[JsonSerializable(typeof(AppUgcHonorTagBriefInfo))]
[JsonSerializable(typeof(AppUgcHonorTagDetail))]
[JsonSerializable(typeof(AppUgcSubTitleItem))]
[JsonSerializable(typeof(AppUgcUser))]
[JsonSerializable(typeof(AppUgcUserBaseInfo))]
[JsonSerializable(typeof(AppUgcUserRelation))]
[JsonSerializable(typeof(AppUgcUserTag))]
[JsonSerializable(typeof(AppUgcUserTitleInfo))]
[JsonSerializable(typeof(AppUploadPictureData))]
[JsonSerializable(typeof(AppUserListInfo))]
[JsonSerializable(typeof(AppUserProfileData))]
[JsonSerializable(typeof(AppUserProfileGetResponse))]
[JsonSerializable(typeof(AppUserProfilePayload))]
[JsonSerializable(typeof(AppUserProfileTab))]
[JsonSerializable(typeof(AppUserRelationListData))]
[JsonSerializable(typeof(AppUserRelationListInfo))]
[JsonSerializable(typeof(AppUserRelationListPayload))]
[JsonSerializable(typeof(AppUserRelationUgcUser))]
[JsonSerializable(typeof(AppUserSubTitleItem))]
[JsonSerializable(typeof(AppUserVideo))]
[JsonSerializable(typeof(AppUserVideoCommon))]
[JsonSerializable(typeof(AppUserVideoExpand))]
[JsonSerializable(typeof(AppUserVideoStat))]
[JsonSerializable(typeof(AppUserWorkRecord))]
[JsonSerializable(typeof(AppVideoMetaDto))]
[JsonSerializable(typeof(AppVipInfoDto))]

// ============================================================================
// 2. App 统一响应包装容器 (AppResponse<T>, A-Z)
// ============================================================================
[JsonSerializable(typeof(AppResponse<AppAlbumData>))]
[JsonSerializable(typeof(AppResponse<AppCatalogData>))]
[JsonSerializable(typeof(AppResponse<AppCommentAddData>))]
[JsonSerializable(typeof(AppResponse<AppCommentListData>))]
[JsonSerializable(typeof(AppResponse<AppCommonAbResultData>))]
[JsonSerializable(typeof(AppResponse<AppCurrentUserInfoData>))]
[JsonSerializable(typeof(AppResponse<AppDanmakuData>))]
[JsonSerializable(typeof(AppResponse<AppDetailsData>))]
[JsonSerializable(typeof(AppResponse<AppEmoticonData>))]
[JsonSerializable(typeof(AppResponse<AppFollowUpdateListData>))]
[JsonSerializable(typeof(AppResponse<AppHistoryData>))]
[JsonSerializable(typeof(AppResponse<AppLoginData>))]
[JsonSerializable(typeof(AppResponse<AppOperationResponse>))]
[JsonSerializable(typeof(AppResponse<AppPlaybackData>))]
[JsonSerializable(typeof(AppResponse<AppReplyAddData>))]
[JsonSerializable(typeof(AppResponse<AppReplyListData>))]
[JsonSerializable(typeof(AppResponse<AppReservationData>))]
[JsonSerializable(typeof(AppResponse<AppShareData>))]
[JsonSerializable(typeof(AppResponse<AppShelfListData>))]
[JsonSerializable(typeof(AppResponse<AppShortFilterData>))]
[JsonSerializable(typeof(AppResponse<AppShortFilterResponse>))]
[JsonSerializable(typeof(AppResponse<AppShortRankDataContainer>))]
[JsonSerializable(typeof(AppResponse<AppTagLandingData>))]
[JsonSerializable(typeof(AppResponse<AppUploadPictureData>))]
[JsonSerializable(typeof(AppResponse<AppUserActionData>))]
[JsonSerializable(typeof(AppResponse<AppUserProfileData>))]
[JsonSerializable(typeof(AppResponse<AppUserProfileGetResponse>))]
[JsonSerializable(typeof(AppResponse<AppUserRelationListData>))]
[JsonSerializable(typeof(AppResponse<AppUserShelfData>))]
[JsonSerializable(typeof(AppResponse<Dictionary<string, AppMultiPlaybackEntry>>))]
[JsonSerializable(typeof(AppResponse<Dictionary<string, AppPlayerSeriesEntry>>))]
[JsonSerializable(typeof(AppResponse<IReadOnlyList<AppHotSearchPlanCellDto>>))]
[JsonSerializable(typeof(AppResponse<IReadOnlyList<AppMultiDetailItem>>))]
[JsonSerializable(typeof(AppResponse<List<AppHotSearchPlanCellDto>>))]

// ============================================================================
// 3. Web 网页端协议数据传输对象 (Web DTOs & Responses, A-Z)
// ============================================================================
[JsonSerializable(typeof(WebCategoryPageDto))]
[JsonSerializable(typeof(WebDetailsPageDto))]
[JsonSerializable(typeof(WebPassportAccountInfoData))]
[JsonSerializable(typeof(WebPassportResponse<WebPassportAccountInfoData>))]
[JsonSerializable(typeof(WebPassportResponse<WebQrCodeData>))]
[JsonSerializable(typeof(WebPassportResponse<WebQrStatusData>))]
[JsonSerializable(typeof(WebPlayerPageDto))]
[JsonSerializable(typeof(WebQrCodeData))]
[JsonSerializable(typeof(WebQrStatusData))]
[JsonSerializable(typeof(WebSearchPageDto))]
[JsonSerializable(typeof(WebSuggestionResponseDto))]

// ============================================================================
// 4. 跨端共享剧集协议实体 (Shared Drama DTOs, A-Z)
// ============================================================================
[JsonSerializable(typeof(DramaDto))]
[JsonSerializable(typeof(DramaEpisodeDto))]
[JsonSerializable(typeof(DramaRelateBookContainerDto))]
[JsonSerializable(typeof(DramaRelateBookInfoDto))]
[JsonSerializable(typeof(DramaSecondaryInfoDto))]
[JsonSerializable(typeof(DramaTagDto))]
[JsonSerializable(typeof(DramaVideoDetailDto))]

// ============================================================================
// 5. 顶层泛型集合容器类型 (Collections, A-Z)
// ============================================================================
[JsonSerializable(typeof(IReadOnlyList<AppCommentImageDataDto>))]
[JsonSerializable(typeof(IReadOnlyList<AppCommentImageDto>))]
[JsonSerializable(typeof(IReadOnlyList<AppHotSearchLynxCellDto>))]
[JsonSerializable(typeof(List<AppHotSearchLynxCellDto>))]
internal partial class SourceGenerationContext : JsonSerializerContext;

internal static class JsonAotSerializer
{
    /// <summary>
    /// 将对象序列化为 JSON 字符串。
    /// </summary>
    /// <param name="value">要序列化的对象</param>
    /// <returns>序列化后的 JSON 字符串</returns>
    internal static string Serialize<T>(T value)
    {
        if (
            SourceGenerationContext.Default.GetTypeInfo(typeof(T))
            is not JsonTypeInfo<T> jsonTypeInfo
        )
        {
            throw new ArgumentNullException(
                nameof(value),
                $"{typeof(T)} is not declared in the SourceGenerationContext attributes."
            );
        }
        return JsonSerializer.Serialize(value, jsonTypeInfo);
    }

    internal static byte[] SerializeToUtf8Bytes<T>(T value)
    {
        if (
            SourceGenerationContext.Default.GetTypeInfo(typeof(T))
            is not JsonTypeInfo<T> jsonTypeInfo
        )
        {
            throw new ArgumentNullException(
                nameof(value),
                $"{typeof(T)} is not declared in the SourceGenerationContext attributes."
            );
        }
        return JsonSerializer.SerializeToUtf8Bytes(value, jsonTypeInfo);
    }

    internal static void Serialize<T>(Utf8JsonWriter writer, T value)
    {
        if (
            SourceGenerationContext.Default.GetTypeInfo(typeof(T))
            is not JsonTypeInfo<T> jsonTypeInfo
        )
        {
            throw new ArgumentNullException(
                nameof(value),
                $"{typeof(T)} is not declared in the SourceGenerationContext attributes."
            );
        }
        JsonSerializer.Serialize(writer, value, jsonTypeInfo);
    }

    /// <summary>
    /// 将 JSON 字符串反序列化为指定类型。
    /// </summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <param name="value">JSON 字符串</param>
    /// <returns>反序列化后的对象</returns>
    internal static T? Deserialize<T>(string value)
    {
        if (
            SourceGenerationContext.Default.GetTypeInfo(typeof(T))
            is not JsonTypeInfo<T> jsonTypeInfo
        )
        {
            throw new ArgumentNullException(
                nameof(T),
                $"{typeof(T)} is not declared in the SourceGenerationContext attributes."
            );
        }
        return JsonSerializer.Deserialize(value, jsonTypeInfo);
    }

    internal static T? Deserialize<T>(ReadOnlySpan<byte> value)
    {
        if (
            SourceGenerationContext.Default.GetTypeInfo(typeof(T))
            is not JsonTypeInfo<T> jsonTypeInfo
        )
        {
            throw new ArgumentNullException(
                nameof(T),
                $"{typeof(T)} is not declared in the SourceGenerationContext attributes."
            );
        }
        return JsonSerializer.Deserialize(value, jsonTypeInfo);
    }
}
