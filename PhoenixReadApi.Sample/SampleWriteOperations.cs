namespace PhoenixReadApi.Sample;

internal static partial class SampleSteps
{
    public static async Task RunWriteOperationsAsync(SampleContext context)
    {
        string marker = $"很不错_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";

        PhoenixReadApiClient client = context.Client;
        CancellationToken cancellationToken = context.CancellationToken;
        string seriesId = context.SeriesId;
        string videoId = context.VideoId;
        Episode? episode = context.FirstEpisode;
        DramaComment? comment = context.SampleComment;
        string userId = context.CreditUserId;

        HashSet<string> favoriteSeriesIds =
            context
                .Favorites?.Items.Select(static item => item.Id)
                .Where(static id => id.Length > 0)
                .ToHashSet(StringComparer.Ordinal)
            ?? [];
        HashSet<string> likedSeriesIds =
            context
                .Likes?.Items.Select(static item => item.Id)
                .Where(static id => id.Length > 0)
                .ToHashSet(StringComparer.Ordinal)
            ?? [];
        ReadHistoryEntry? existingHistory = context.History?.Items.FirstOrDefault(item =>
            item.SeriesId == seriesId && item.VideoId == videoId
        );
        HashSet<string> existingReviewIds =
            context.Reviews?.Items.Select(static item => item.Id).ToHashSet(StringComparer.Ordinal)
            ?? [];
        HashSet<string> existingEpisodeCommentIds =
            context
                .EpisodeComments?.Items.Select(static item => item.Id)
                .ToHashSet(StringComparer.Ordinal)
            ?? [];
        HashSet<string> existingDanmakuIds =
            context.Danmaku?.Items.Select(static item => item.Id).ToHashSet(StringComparer.Ordinal)
            ?? [];
        HashSet<string> existingReplyIds =
            context.Replies?.Items.Select(static item => item.Id).ToHashSet(StringComparer.Ordinal)
            ?? [];

        if (seriesId.Length > 0 && videoId.Length > 0)
        {
            await TryAsync(
                "ReportProgressAsync",
                () =>
                    client.ReportProgressAsync(
                        new ProgressUpdate
                        {
                            SeriesId = seriesId,
                            VideoId = videoId,
                            EpisodeNumber = episode?.Number ?? 0,
                            CurrentPositionMs = 1_000,
                            DurationMs = 60_000,
                            AccumulatedPlayTimeMs = 1_000,
                        },
                        cancellationToken
                    )
            );
            bool wasFavorite = favoriteSeriesIds.Contains(seriesId);
            await TryAsync(
                "SetFavoriteAsync",
                () => client.SetFavoriteAsync(seriesId, videoId, true, cancellationToken)
            );
            await TryAsync(
                "RemoveFavoriteAsync",
                () => client.RemoveFavoriteAsync(seriesId, videoId, cancellationToken)
            );
            if (wasFavorite)
            {
                await TryAsync(
                    "SetFavoriteAsync(restore)",
                    () => client.SetFavoriteAsync(seriesId, videoId, true, cancellationToken)
                );
            }

            bool wasLiked = likedSeriesIds.Contains(seriesId);
            await TryAsync(
                "SetDramaEpisodeLikeAsync",
                () => client.SetDramaEpisodeLikeAsync(seriesId, videoId, true, cancellationToken)
            );
            await TryAsync(
                "SetDramaEpisodeLikeAsync(false)",
                () => client.SetDramaEpisodeLikeAsync(seriesId, videoId, false, cancellationToken)
            );
            if (wasLiked)
            {
                await TryAsync(
                    "SetDramaLikeAsync(restore)",
                    () =>
                        client.SetDramaEpisodeLikeAsync(seriesId, videoId, true, cancellationToken)
                );
            }
            // 1. 发表分集评论
            string commentText = $"分集评论_{marker}";
            PhoenixOperationResult? commentResult = await TryAsync(
                "SendEpisodeCommentAsync",
                () => client.SendEpisodeCommentAsync(seriesId, videoId, commentText, cancellationToken)
            );
            string createdCommentId = commentResult?.Id ?? string.Empty;
            if (string.IsNullOrWhiteSpace(createdCommentId))
            {
                throw new InvalidOperationException("发表分集评论未返回有效 Comment ID，测试失败。");
            }

            // 2. 在自己刚刚发的评论里发表回复
            string replyText = $"回复_{marker}";
            PhoenixOperationResult? replyResult = await TryAsync(
                "SendCommentReplyAsync(on self comment)",
                () => client.SendCommentReplyAsync(createdCommentId, videoId, replyText, "", cancellationToken)
            );
            string createdReplyId = replyResult?.Id ?? string.Empty;
            if (string.IsNullOrWhiteSpace(createdReplyId))
            {
                throw new InvalidOperationException("对自发评论发表回复未返回有效 Reply ID，测试失败。");
            }

            // 3. 对自建回复进行点赞与取消点赞
            await TryAsync(
                "SetReplyLikeAsync(created)",
                () => client.SetReplyLikeAsync(createdReplyId, videoId, true, cancellationToken)
            );
            await TryAsync(
                "SetReplyLikeAsync(created,false)",
                () => client.SetReplyLikeAsync(createdReplyId, videoId, false, cancellationToken)
            );

            // 4. 删除时先删回复！
            PhoenixOperationResult? deleteReplyResult = await TryAsync(
                "DeleteReplyAsync(created)",
                () => client.DeleteReplyAsync(createdReplyId, cancellationToken)
            );
            if (deleteReplyResult is null || !deleteReplyResult.Succeeded)
            {
                throw new InvalidOperationException($"删除回复失败（Reply ID: {createdReplyId}），测试失败。");
            }

            // 5. 再删自己发的评论！
            PhoenixOperationResult? deleteCommentResult = await TryAsync(
                "DeleteCommentAsync(created)",
                () => client.DeleteCommentAsync(createdCommentId, videoId, cancellationToken)
            );
            if (deleteCommentResult is null || !deleteCommentResult.Succeeded)
            {
                throw new InvalidOperationException($"删除评论失败（Comment ID: {createdCommentId}），测试失败。");
            }

            // 6. 发表弹幕
            string danmakuText = $"弹幕_{marker}";
            PhoenixOperationResult? danmakuResult = await TryAsync(
                "SendDanmakuAsync",
                () => client.SendDanmakuAsync(seriesId, videoId, danmakuText, cancellationToken)
            );
            string createdDanmakuId = danmakuResult?.Id ?? string.Empty;
            if (string.IsNullOrWhiteSpace(createdDanmakuId))
            {
                throw new InvalidOperationException("发表弹幕未返回有效 Danmaku ID，测试失败。");
            }

            // 7. 删除弹幕
            PhoenixOperationResult? deleteDanmakuResult = await TryAsync(
                "DeleteDanmakuAsync(created)",
                () => client.DeleteDanmakuAsync(createdDanmakuId, videoId, cancellationToken)
            );
            if (deleteDanmakuResult is null || !deleteDanmakuResult.Succeeded)
            {
                throw new InvalidOperationException($"删除弹幕失败（Danmaku ID: {createdDanmakuId}），测试失败。");
            }
        }

        if (seriesId.Length > 0)
        {
            // 8. 剧评：发表与删除生命周期测试
            string reviewSeriesId = seriesId;
            string reviewText = $"剧评_{marker}_{Guid.NewGuid().ToString("N")[..6]}";
            PhoenixOperationResult? reviewResult = null;
            try
            {
                reviewResult = await client.SendDramaReviewAsync(reviewSeriesId, 5.0, reviewText, cancellationToken);
            }
            catch (PhoenixReadApiProviderException ex) when (ex.ProviderCode == "103009")
            {
                Console.WriteLine($"  [提示] 剧集 {reviewSeriesId} 历史已评过 (103009)，选用备选剧集测试剧评发表与删除生命周期...");
                string? alternativeSeriesId = context.Catalog?.ElementAtOrDefault(10)?.Id
                    ?? context.Catalog?.LastOrDefault()?.Id;
                if (!string.IsNullOrEmpty(alternativeSeriesId))
                {
                    reviewSeriesId = alternativeSeriesId;
                    string altReviewText = $"剧评_{marker}_{Guid.NewGuid().ToString("N")[..6]}";
                    Console.WriteLine($"  [切换] 使用未评价短剧 {reviewSeriesId} 测试剧评发布与删除...");
                    reviewResult = await client.SendDramaReviewAsync(reviewSeriesId, 5.0, altReviewText, cancellationToken);
                }
                else
                {
                    throw;
                }
            }

            string createdReviewId = reviewResult?.Id ?? string.Empty;
            if (string.IsNullOrWhiteSpace(createdReviewId))
            {
                throw new InvalidOperationException("发表剧评未返回有效 Review ID，测试失败。");
            }

            // 9. 删除剧评！删除失败纳入测试失败范畴
            PhoenixOperationResult? deleteReviewResult = await TryAsync(
                "DeleteDramaReviewAsync(created)",
                () => client.DeleteDramaReviewAsync(createdReviewId, reviewSeriesId, cancellationToken)
            );
            if (deleteReviewResult is null || !deleteReviewResult.Succeeded)
            {
                throw new InvalidOperationException($"删除剧评失败（Review ID: {createdReviewId}），测试失败。");
            }

            string reservationSeriesId =
                context.UpcomingReservations?.Items.FirstOrDefault()?.SeriesId ?? seriesId;
            if (reservationSeriesId.Length > 0)
            {
                await TryAsync(
                    "SetReservationAsync",
                    () => client.SetReservationAsync(reservationSeriesId, true, cancellationToken)
                );
                await TryAsync(
                    "DeleteReservationAsync",
                    () => client.DeleteReservationAsync(reservationSeriesId, cancellationToken)
                );
            }
        }

        if (comment is not null)
        {
            await TryAsync(
                "SetCommentLikeAsync",
                () => client.SetCommentLikeAsync(comment.Id, seriesId, true, cancellationToken)
            );
            await TryAsync(
                "SetCommentLikeAsync(false)",
                () => client.SetCommentLikeAsync(comment.Id, seriesId, false, cancellationToken)
            );
            await TryAsync(
                "SetReviewLikeAsync",
                () => client.SetReviewLikeAsync(comment.Id, seriesId, true, cancellationToken)
            );
            await TryAsync(
                "SetReviewLikeAsync(false)",
                () => client.SetReviewLikeAsync(comment.Id, seriesId, false, cancellationToken)
            );
            string? sampleDanmakuId = context.Danmaku?.Items.FirstOrDefault()?.Id;
            if (!string.IsNullOrEmpty(sampleDanmakuId))
            {
                await TryAsync(
                    "SetDanmakuLikeAsync",
                    () => client.SetDanmakuLikeAsync(sampleDanmakuId, videoId, true, cancellationToken)
                );
                await TryAsync(
                    "SetDanmakuLikeAsync(false)",
                    () => client.SetDanmakuLikeAsync(sampleDanmakuId, videoId, false, cancellationToken)
                );
            }
        }

        if (userId.Length > 0)
        {
            await TryAsync(
                "SetFollowAsync",
                () => client.SetFollowAsync(userId, true, cancellationToken)
            );
            await TryAsync(
                "SetFollowAsync(false)",
                () => client.SetFollowAsync(userId, false, cancellationToken)
            );
        }

        if (episode is not null && seriesId.Length > 0 && videoId.Length > 0)
        {
            await TryAsync(
                "DeleteHistoryAsync",
                () => client.DeleteHistoryAsync(seriesId, videoId, cancellationToken)
            );
            if (existingHistory is not null)
            {
                await TryAsync(
                    "ReportProgressAsync(restore)",
                    () =>
                        client.ReportProgressAsync(
                            new ProgressUpdate
                            {
                                SeriesId = existingHistory.SeriesId,
                                VideoId = existingHistory.VideoId,
                                EpisodeNumber = episode.Number,
                                CurrentPositionMs = existingHistory.CurrentPositionMs,
                                DurationMs = existingHistory.DurationMs,
                                AccumulatedPlayTimeMs = existingHistory.CurrentPositionMs,
                            },
                            cancellationToken
                        )
                );
            }
        }
    }
}
