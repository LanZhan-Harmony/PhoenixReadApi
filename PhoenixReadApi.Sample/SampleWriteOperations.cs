namespace PhoenixReadApi.Sample;

internal static partial class SampleSteps
{
    public static async Task RunWriteOperationsAsync(SampleContext context)
    {
        string marker = $"很不错_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";

        PhoenixReadApiClient client = context.Client;
        CancellationToken cancellationToken = context.CancellationToken;
        string dramaId = context.DramaId;
        string videoId = context.VideoId;
        Episode? episode = context.FirstEpisode;
        Comment? comment = context.SampleComment;
        string userId = context.CreditUserId;

        HashSet<string> favoriteDramaIds = context
            .Favorites?.Select(static item => item.Id)
            .Where(static id => id.Length > 0)
            .ToHashSet(StringComparer.Ordinal)
            ?? [];
        HashSet<string> likedDramaIds = context
            .Likes?.Items.Select(static item => item.Id)
            .Where(static id => id.Length > 0)
            .ToHashSet(StringComparer.Ordinal)
            ?? [];
        HistoryItem<Drama>? existingHistory = context.History?.Items.FirstOrDefault(item =>
            item.Target.Id == dramaId && item.VideoId == videoId
        );
        HashSet<string> existingReviewIds = context
            .Reviews?.Items.Select(static item => item.Id)
            .ToHashSet(StringComparer.Ordinal)
            ?? [];
        HashSet<string> existingEpisodeCommentIds = context
            .EpisodeComments?.Items.Select(static item => item.Id)
            .ToHashSet(StringComparer.Ordinal)
            ?? [];
        HashSet<string> existingDanmakuIds = context
            .Danmaku?.Items.Select(static item => item.Id)
            .ToHashSet(StringComparer.Ordinal)
            ?? [];
        HashSet<string> existingReplyIds = context
            .Replies?.Items.Select(static item => item.Id)
            .ToHashSet(StringComparer.Ordinal)
            ?? [];

        if (dramaId.Length > 0 && videoId.Length > 0)
        {
            await TryAsync("ReportHistoryAsync", () => client.ReportHistoryAsync(
                    HistoryUpdate.FromDrama(
                        dramaId,
                        videoId,
                        episode?.Number ?? 1,
                        currentPositionMs: 1_000,
                        accumulatedPlayTimeMs: 1_000
                    ),
                    cancellationToken
                ));
            await TryAsync("ReportProgressAsync", () => client.ReportProgressAsync(
                    ProgressUpdate.FromDrama(
                        dramaId,
                        videoId,
                        currentPlayTimeMs: 1_000,
                        cumulativeTotalDurationMs: 60_000
                    ),
                    cancellationToken
                ));
            HistoryPage<Drama>? history = await TryAsync("GetDramaHistoryAsync", () =>
                client.GetDramaHistoryAsync(cancellationToken: cancellationToken)
            );
            PrintCount("  历史记录数", history?.Items.Count);
            await TryAsync("SetFavoriteAsync", () =>
                client.SetFavoriteAsync(
                    FavoriteTarget.FromDrama(dramaId, videoId),
                    true,
                    cancellationToken
                )
            );
            await TryAsync("SetFavoriteAsync(false)", () =>
                client.SetFavoriteAsync(
                    FavoriteTarget.FromDrama(dramaId, videoId),
                    false,
                    cancellationToken
                )
            );

            await TryAsync("SetDramaEpisodeLikeAsync", () =>
                client.SetDramaEpisodeLikeAsync(dramaId, videoId, true, cancellationToken)
            );
            await TryAsync("SetDramaEpisodeLikeAsync(false)", () =>
                client.SetDramaEpisodeLikeAsync(dramaId, videoId, false, cancellationToken)
            );

            CommentTarget episodeTarget = CommentTarget.FromEpisode(dramaId, videoId);
            // 1. 发表分集评论
            string commentText = $"分集评论_{marker}";
            Comment? commentResult = await TryAsync(
                "SendCommentAsync(Episode)",
                () => client.SendCommentAsync(episodeTarget, commentText, cancellationToken)
            );
            string createdCommentId = commentResult?.Id ?? string.Empty;
            if (string.IsNullOrWhiteSpace(createdCommentId))
            {
                Console.WriteLine(
                    "  [提示] 发表分集评论未返回有效 ID（可能由于网关抖动或审核排队），跳过自建回复与删除链路。"
                );
            }
            else
            {
                // 2. 在自己刚刚发的评论里发表回复
                string replyText = $"回复_{marker}";
                CommentReply? replyResult = await TryAsync(
                    "SendCommentReplyAsync(on self comment)",
                    () =>
                        client.SendCommentReplyAsync(
                            episodeTarget,
                            createdCommentId,
                            replyText,
                            "",
                            cancellationToken
                        )
                );
                string createdReplyId = replyResult?.Id ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(createdReplyId))
                {
                    // 3. 对自建回复进行点赞与取消点赞
                    await TryAsync("SetReplyLikeAsync(created)", () =>
                        client.SetReplyLikeAsync(
                            episodeTarget,
                            createdReplyId,
                            true,
                            cancellationToken
                        )
                    );
                    await TryAsync("SetReplyLikeAsync(created,false)", () =>
                        client.SetReplyLikeAsync(
                            episodeTarget,
                            createdReplyId,
                            false,
                            cancellationToken
                        )
                    );

                    // 4. 删除时先删回复！
                    await TryAsync("DeleteReplyAsync(created)", () =>
                        client.DeleteReplyAsync(episodeTarget, createdReplyId, cancellationToken)
                    );
                }

                // 5. 再删自己发的评论！
                await TryAsync("DeleteCommentAsync(created)", () =>
                    client.DeleteCommentAsync(episodeTarget, createdCommentId, cancellationToken)
                );
            }

            // 6. 发表弹幕
            string danmakuText = $"弹幕_{marker}";
            DanmakuItem? danmakuResult = await TryAsync("SendDanmakuAsync", () =>
                client.SendDramaEpisodeDanmakuAsync(
                    dramaId,
                    videoId,
                    danmakuText,
                    offsetMs: 0,
                    cancellationToken
                )
            );
            string createdDanmakuId = danmakuResult?.Id ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(createdDanmakuId))
            {
                // 7. 删除弹幕
                await TryAsync("DeleteDanmakuAsync(created)", () =>
                    client.DeleteDanmakuAsync(createdDanmakuId, videoId, cancellationToken)
                );
            }
        }

        if (dramaId.Length > 0)
        {
            // 8. 剧评：发表与删除生命周期测试
            string reviewDramaId = dramaId;
            string reviewText = $"剧评_{marker}_{Guid.NewGuid().ToString("N")[..6]}";
            DramaReview? reviewResult = null;
            try
            {
                reviewResult = await client.SendDramaReviewAsync(
                    reviewDramaId,
                    5.0,
                    reviewText,
                    cancellationToken
                );
            }
            catch (PhoenixReadApiProviderException ex) when (ex.ProviderCode == "103009")
            {
                Console.WriteLine(
                    $"  [提示] 剧集 {reviewDramaId} 历史已评过 (103009)，选用备选剧集测试剧评发表与删除生命周期..."
                );
                string? alternativeDramaId = context.Catalog?.ElementAtOrDefault(10)?.Id
                    ?? context.Catalog?.LastOrDefault()?.Id;
                if (!string.IsNullOrEmpty(alternativeDramaId))
                {
                    reviewDramaId = alternativeDramaId;
                    string altReviewText = $"剧评_{marker}_{Guid.NewGuid().ToString("N")[..6]}";
                    Console.WriteLine(
                        $"  [切换] 使用未评价短剧 {reviewDramaId} 测试剧评发布与删除..."
                    );
                    reviewResult = await client.SendDramaReviewAsync(
                        reviewDramaId,
                        5.0,
                        altReviewText,
                        cancellationToken
                    );
                }
                else
                {
                    throw;
                }
            }

            string createdReviewId = reviewResult?.Id ?? string.Empty;
            if (string.IsNullOrWhiteSpace(createdReviewId))
            {
                Console.WriteLine(
                    "  [提示] 发表剧评未返回有效 Review ID（可能由于网关抖动或审核排队），跳过删除剧评。"
                );
            }
            else
            {
                // 9. 删除剧评
                await TryAsync("DeleteCommentAsync(DramaReview)", () =>
                    client.DeleteCommentAsync(
                        CommentTarget.FromDramaReview(reviewDramaId),
                        createdReviewId,
                        cancellationToken
                    )
                );
            }

            string reservationDramaId = context
                .UpcomingReservations?.Items.FirstOrDefault()
                ?.DramaId
                ?? dramaId;
            if (reservationDramaId.Length > 0)
            {
                await TryAsync("SetReservationAsync", () =>
                    client.SetReservationAsync(reservationDramaId, true, cancellationToken)
                );
                await TryAsync("DeleteReservationAsync", () =>
                    client.DeleteReservationAsync(reservationDramaId, cancellationToken)
                );
            }
        }

        if (comment is not null)
        {
            CommentTarget epTarget = CommentTarget.FromEpisode(dramaId, videoId);
            CommentTarget reviewTarget = CommentTarget.FromDramaReview(dramaId);
            await TryAsync("SetCommentLikeAsync", () =>
                client.SetCommentLikeAsync(epTarget, comment.Id, true, cancellationToken)
            );
            await TryAsync("SetCommentLikeAsync(false)", () =>
                client.SetCommentLikeAsync(epTarget, comment.Id, false, cancellationToken)
            );
            await TryAsync("SetReviewLikeAsync", () =>
                client.SetCommentLikeAsync(reviewTarget, comment.Id, true, cancellationToken)
            );
            await TryAsync("SetReviewLikeAsync(false)", () =>
                client.SetCommentLikeAsync(reviewTarget, comment.Id, false, cancellationToken)
            );
            string? sampleDanmakuId = context.Danmaku?.Items.FirstOrDefault()?.Id;
            if (!string.IsNullOrEmpty(sampleDanmakuId))
            {
                await TryAsync("SetDanmakuLikeAsync", () =>
                    client.SetDanmakuLikeAsync(sampleDanmakuId, videoId, true, cancellationToken)
                );
                await TryAsync("SetDanmakuLikeAsync(false)", () =>
                    client.SetDanmakuLikeAsync(sampleDanmakuId, videoId, false, cancellationToken)
                );
            }
        }

        if (userId.Length > 0)
        {
            await TryAsync("SetFollowAsync", () =>
                client.SetFollowAsync(userId, true, cancellationToken)
            );
            await TryAsync("SetFollowAsync(false)", () =>
                client.SetFollowAsync(userId, false, cancellationToken)
            );
        }

        if (episode is not null && dramaId.Length > 0 && videoId.Length > 0)
        {
            await TryAsync("DeleteHistoryAsync", () =>
                client.DeleteHistoryAsync(
                    [HistoryDeleteEntry.FromDrama(dramaId, videoId)],
                    cancellationToken
                )
            );
        }
    }
}
