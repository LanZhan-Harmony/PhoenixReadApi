using System.Net;
using System.Text;

namespace PhoenixReadApi.Tests;

/// <summary>
/// 7 路强类型搜索、C# 15 原生 Union、红果圈模型复用与短视频直接解析的确定性测试。
/// </summary>
public sealed class AlbumAndShortVideoTests
{
    [Test]
    public async Task TestSearchUsersAsync_MapsUserAvatarField()
    {
        const string searchJson = """
            {
              "code": 0,
              "message": "success",
              "search_tabs": [
                {
                  "tab_type": 27,
                  "data": [
                    {
                      "celebrity_user_list": [
                        {
                          "user_id": "3112051666323866",
                          "base_info": {
                            "user_id": "3112051666323866",
                            "user_name": "刘念",
                            "user_avatar": "https://example.com/avatar-liunian.jpg",
                            "desc": "短剧演员",
                            "fans_cnt": 123456
                          }
                        }
                      ]
                    }
                  ],
                  "has_more": false
                }
              ]
            }
            """;

        using MockHandler handler = new(
            (req, _) =>
            {
                string path = req.RequestUri?.AbsolutePath ?? string.Empty;
                return path.Contains("/search/tab/v") ? searchJson : "{}";
            }
        );

        using HttpClient httpClient = new(handler);
        using PhoenixReadApiClient client = new(httpClient);

        SearchPage<User> result = await client.SearchUsersAsync("刘念");

        await Assert.That(result.Items.Count).IsEqualTo(1);
        await Assert.That(result.Items[0].Id).IsEqualTo("3112051666323866");
        await Assert.That(result.Items[0].Name).IsEqualTo("刘念");
        await Assert
            .That(result.Items[0].AvatarUrl)
            .IsEqualTo("https://example.com/avatar-liunian.jpg");
    }

    [Test]
    public async Task TestSearchVideosAsync_ReturnsUnionOfDramaAlbumAndShortVideo()
    {
        const string searchJson = """
            {
              "code": 0,
              "message": "success",
              "search_tabs": [
                {
                  "tab_type": 30,
                  "data": [
                    {
                      "video_data": [
                        {
                          "album_info": {
                            "album_id": 7706480483017690137,
                            "album_id_str": "7706480483017690137",
                            "title": "娇女诡爱畸恋",
                            "intro": "合集简介",
                            "cover": "https://example.com/album-cover.jpg",
                            "episode_cnt": 51
                          }
                        },
                        {
                          "series_id": 7689008778074655806,
                          "series_id_str": "7689008778074655806",
                          "series_title": "独立短视频",
                          "series_cover": "https://example.com/short-cover.jpg",
                          "content_type": 1001,
                          "duration": 189
                        }
                      ]
                    }
                  ],
                  "has_more": false
                }
              ]
            }
            """;

        const string albumJson = """
            {
              "code": 0,
              "message": "success",
              "data": {
                "album_data": {
                  "album_id": 7706480483017690137,
                  "album_id_str": "7706480483017690137",
                  "title": "娇女诡爱畸恋",
                  "cover": "https://example.com/album-cover.jpg",
                  "intro": "合集简介",
                  "episode_cnt": 51,
                  "status": 1,
                  "video_detail_list": [
                    {
                      "series_id": 7706480472527752216,
                      "series_id_str": "7706480472527752216",
                      "series_title": "第1条短视频",
                      "series_cover": "https://example.com/sub1.jpg",
                      "episode_cnt": 1
                    }
                  ]
                }
              }
            }
            """;

        using MockHandler handler = new(
            (req, _) =>
            {
                string path = req.RequestUri?.AbsolutePath ?? string.Empty;
                if (path.Contains("/search/tab/v"))
                {
                    return searchJson;
                }
                if (path.Contains("/novel/player/album_detail/v1"))
                {
                    return albumJson;
                }
                return "{}";
            }
        );

        using HttpClient httpClient = new(handler);
        using PhoenixReadApiClient client = new(httpClient);

        SearchPage<VideoSearchItem> videos = await client.SearchVideosAsync("娇女诡爱畸恋");
        await Assert.That(videos.Items.Count).IsEqualTo(2);

        VideoAlbum? albumHit = videos.Items[0].Value as VideoAlbum;
        ShortVideo? shortHit = videos.Items[1].Value as ShortVideo;
        await Assert.That(albumHit).IsNotNull();
        await Assert.That(shortHit).IsNotNull();
        await Assert.That(albumHit!.Id).IsEqualTo("7706480483017690137");
        await Assert.That(shortHit!.VideoId).IsEqualTo("7689008778074655806");
        await Assert.That(shortHit.DurationSeconds).IsEqualTo(189);

        VideoAlbumDetails albumDetails = await client.GetVideoAlbumDetailsAsync(albumHit.Id);
        await Assert.That(albumDetails.Album.Id).IsEqualTo("7706480483017690137");
        await Assert.That(albumDetails.Videos.Count).IsEqualTo(1);
        await Assert.That(albumDetails.Videos[0].VideoId).IsEqualTo("7706480472527752216");
    }

    [Test]
    public async Task TestSearchDiscussionsAndAudiobooksAsync_MapsPostDataAndAudiobook()
    {
        const string discussionJson = """
            {
              "code": 0,
              "message": "success",
              "search_tabs": [
                {
                  "tab_type": 31,
                  "data": [
                    {
                      "post_data": [
                        {
                          "post_id": "7680591687546274366",
                          "title": "安利帖",
                          "pure_content": "很好看！",
                          "reply_cnt": 12,
                          "digg_cnt": 88,
                          "create_time": 1795000000,
                          "cover_url": "https://example.com/post-cover.jpg",
                          "user_info": {
                            "user_id": "1001",
                            "user_name": "追剧达人",
                            "user_avatar": "https://example.com/u1.jpg"
                          },
                          "book_card": []
                        }
                      ]
                    }
                  ],
                  "has_more": false
                }
              ]
            }
            """;

        const string audioJson = """
            {
              "code": 0,
              "message": "success",
              "search_tabs": [
                {
                  "tab_type": 2,
                  "data": [
                    {
                      "book_data": [
                        {
                          "book_id": "7102740776311852039",
                          "book_name": "恐怖女人村",
                          "author": "李道长（主播：久界）",
                          "abstract": "悬疑故事",
                          "category": "悬疑灵异",
                          "genre": "4",
                          "book_type": "1",
                          "audio_thumb_uri": "https://example.com/audio.jpg",
                          "score": "7.9",
                          "serial_count": "146",
                          "creation_status": "0"
                        }
                      ]
                    }
                  ],
                  "has_more": false
                }
              ]
            }
            """;

        using MockHandler handler = new(
            (req, _) =>
            {
                string query = req.RequestUri?.Query ?? string.Empty;
                if (query.Contains("tab_type=31"))
                {
                    return discussionJson;
                }
                if (query.Contains("tab_type=2"))
                {
                    return audioJson;
                }
                return "{}";
            }
        );

        using HttpClient httpClient = new(handler);
        using PhoenixReadApiClient client = new(httpClient);

        SearchPage<Post> posts = await client.SearchDiscussionsAsync("娇女诡爱畸恋");
        await Assert.That(posts.Items.Count).IsEqualTo(1);
        await Assert.That(posts.Items[0].Id).IsEqualTo("7680591687546274366");
        await Assert.That(posts.Items[0].Title).IsEqualTo("安利帖");
        await Assert.That(posts.Items[0].LikeCount).IsEqualTo(88);

        SearchPage<Audiobook> books = await client.SearchAudiobooksAsync("恐怖女人村");
        await Assert.That(books.Items.Count).IsEqualTo(1);
        await Assert.That(books.Items[0].Id).IsEqualTo("7102740776311852039");
        await Assert.That(books.Items[0].CoverUrl).IsEqualTo("https://example.com/audio.jpg");
        await Assert.That(books.Items[0].Status).IsEqualTo(NovelCreationStatus.Completed);
    }

    [Test]
    public async Task TestResolveShortVideoMediaAsync_UsesContentType1001Directly()
    {
        const string registerJson = """
            {
              "device_id": 1234567890,
              "install_id": 9876543210,
              "device_id_str": "1234567890",
              "install_id_str": "9876543210"
            }
            """;

        const string successJson = """
            {
              "code": 0,
              "message": "请求成功",
              "data": {
                "video_model": "{\"status\":10,\"message\":\"success\",\"version\":3,\"video_id\":\"v02ebeg10000test\",\"video_duration\":60,\"video_list\":[{\"main_url\":\"https://cdn.example.com/short-720p.mp4\",\"backup_url\":\"https://bak.example.com/short-720p.mp4\",\"video_meta\":{\"definition\":\"720p\",\"vwidth\":720,\"vheight\":1280,\"bitrate\":500000,\"size\":1024000,\"codec_type\":\"h264\"}}]}"
              }
            }
            """;

        List<string> videoModelBodies = [];

        using MockHandler handler = new(
            (req, body) =>
            {
                string path = req.RequestUri?.AbsolutePath ?? string.Empty;
                if (path.Contains("device_register"))
                {
                    return registerJson;
                }
                if (path.Contains("/novel/player/video_model/v1/"))
                {
                    videoModelBodies.Add(body ?? string.Empty);
                    return successJson;
                }
                return string.Empty;
            }
        );

        using HttpClient httpClient = new(handler);
        using PhoenixReadApiClient client = new(httpClient);

        var media = await client.ResolveShortVideoMediaAsync("7698243408908782616");

        await Assert.That(media.Count).IsEqualTo(2);
        await Assert.That(media[0].Url).IsEqualTo("https://cdn.example.com/short-720p.mp4");
        await Assert.That(videoModelBodies.Count).IsEqualTo(1);
        await Assert.That(videoModelBodies[0].Contains("\"content_type\":1001")).IsTrue();
    }

    [Test]
    public async Task TestHistoryNovelAudiobookAndBatchDelete_MapsAndSerializesCorrectly()
    {
        const string novelHistoryJson = """
            {
              "code": 0,
              "message": "SUCCESS",
              "data": {
                "data_list": [
                  {
                    "book_id": 7486750519621323800,
                    "book_type": 0,
                    "genre_type": 0,
                    "read_timestamp_ms": 1795697177905
                  },
                  {
                    "book_id": 7680864609909083198,
                    "book_type": 0,
                    "genre_type": 110,
                    "read_timestamp_ms": 1795697095579
                  }
                ],
                "next_offset": 2,
                "has_more": false,
                "total": 2
              }
            }
            """;

        const string audioHistoryJson = """
            {
              "code": 0,
              "message": "SUCCESS",
              "data": {
                "data_list": [
                  {
                    "book_id": 7359167683255880728,
                    "book_type": 1,
                    "listen_book_id": "7450063007658298393",
                    "read_timestamp_ms": 1795697300917
                  }
                ],
                "next_offset": 1,
                "has_more": false,
                "total": 1
              }
            }
            """;

        const string deleteSuccessJson =
            """{"code":0,"message":"SUCCESS","data":{"update_fail_datas":[]}}""";

        string? capturedDeleteBody = null;

        using MockHandler handler = new(
            (req, body) =>
            {
                string path = req.RequestUri?.AbsolutePath ?? string.Empty;
                string query = req.RequestUri?.Query ?? string.Empty;
                if (path.Contains("/read_history/list/v"))
                {
                    if (query.Contains("book_type=0"))
                        return novelHistoryJson;
                    if (query.Contains("book_type=1"))
                        return audioHistoryJson;
                }
                if (path.Contains("/read_history/update/v"))
                {
                    capturedDeleteBody = body;
                    return deleteSuccessJson;
                }
                return "{}";
            }
        );

        using HttpClient httpClient = new(handler);
        using PhoenixReadApiClient client = new(httpClient);

        var novelPage = await client.GetNovelHistoryAsync();
        await Assert.That(novelPage.Items.Count).IsEqualTo(2);
        await Assert.That(novelPage.Items[0].Target.Id).IsEqualTo("7486750519621323800");
        await Assert.That(novelPage.Items[1].Target.Tags.Contains("漫画")).IsTrue();

        var audioPage = await client.GetAudiobookHistoryAsync();
        await Assert.That(audioPage.Items.Count).IsEqualTo(1);
        await Assert.That(audioPage.Items[0].Target.Id).IsEqualTo("7359167683255880728");
        await Assert.That(audioPage.Items[0].Target.ListenBookId).IsEqualTo("7450063007658298393");

        PhoenixOperationResult delResult = await client.DeleteHistoryAsync([
            HistoryDeleteEntry.FromDrama("7605917652308986904", "7605919517348219966"),
            HistoryDeleteEntry.FromItem(audioPage.Items[0]),
            HistoryDeleteEntry.FromPost("7684461338474578456"),
        ]);
        await Assert.That(delResult.Succeeded).IsTrue();
        await Assert.That(capturedDeleteBody).IsNotNull();
        await Assert.That(capturedDeleteBody!.Contains("\"book_type\":2")).IsTrue();
        await Assert.That(capturedDeleteBody.Contains("\"book_type\":1")).IsTrue();
        await Assert
            .That(capturedDeleteBody.Contains("\"listen_book_id\":\"7450063007658298393\""))
            .IsTrue();
        await Assert.That(capturedDeleteBody.Contains("\"book_type\":5")).IsTrue();
    }

    [Test]
    public async Task ShortVideoDanmakuAndPostShortVideoCommentActions_BuildExpectedPayloads()
    {
        const string danmakuListJson = """
            {
              "code": 0,
              "message": "success",
              "data": {
                "data_list": [
                  {
                    "comment": {
                      "comment_id": "7712450675655525144",
                      "common": {
                        "group_id": "7706480472527752216",
                        "status": "1",
                        "content": { "text": "哈哈哈[笑]" }
                      },
                      "expand": { "offset_time": 42121 }
                    }
                  }
                ],
                "list_info": {
                  "total": 1,
                  "cursor": { "danmaku_count": 1, "end_offset_time": 60000 }
                },
                "extra": { "next_query_danmaku_list_time": 60000 }
              }
            }
            """;

        const string addCommentJson =
            """{"code":0,"message":"success","data":{"comment_info":{"comment_id":"7712448761013797694"}}}""";
        const string addReplyJson =
            """{"code":0,"message":"success","data":{"reply":{"reply_id":"7712451462601130776"}}}""";
        const string okJson = """{"code":0,"message":"success","data":{}}""";

        List<(string Path, string Body)> requests = [];

        using MockHandler handler = new(
            (req, body) =>
            {
                string path = req.RequestUri?.AbsolutePath ?? string.Empty;
                requests.Add((path, body ?? string.Empty));
                if (path.Contains("/comment/list/"))
                    return danmakuListJson;
                if (path.Contains("/comment/add/"))
                    return addCommentJson;
                if (path.Contains("/reply/add/"))
                    return addReplyJson;
                return okJson;
            }
        );

        using HttpClient httpClient = new(handler);
        using PhoenixReadApiClient client = new(httpClient);

        DanmakuPage dmPage = await client.GetShortVideoDanmakuAsync(
            "7706480472527752216",
            0,
            60000
        );
        await Assert.That(dmPage.Items.Count).IsEqualTo(1);
        await Assert.That(dmPage.Items[0].Id).IsEqualTo("7712450675655525144");
        await Assert.That(dmPage.Items[0].OffsetMs).IsEqualTo(42121);

        DanmakuItem sendDm = await client.SendShortVideoDanmakuAsync(
            "7706480472527752216",
            "test dm",
            42121
        );
        await Assert.That(sendDm.Id).IsEqualTo("7712448761013797694");
        await Assert.That(sendDm.Text).IsEqualTo("test dm");
        await Assert.That(sendDm.OffsetMs).IsEqualTo(42121);

        CommentTarget svTarget = CommentTarget.FromShortVideo("7706480472527752216");
        Comment sendSvComment = await client.SendCommentAsync(svTarget, "sv cmt");
        await Assert.That(sendSvComment.Id).IsEqualTo("7712448761013797694");
        await Assert.That(sendSvComment.Text).IsEqualTo("sv cmt");

        CommentReply sendSvReply = await client.SendCommentReplyAsync(
            svTarget,
            "7712448761013797694",
            "sv reply"
        );
        await Assert.That(sendSvReply.Id).IsEqualTo("7712451462601130776");
        await Assert.That(sendSvReply.Text).IsEqualTo("sv reply");

        await client.DeleteReplyAsync(svTarget, "7712451462601130776");
        await client.DeleteCommentAsync(svTarget, "7712448761013797694");

        CommentTarget postTarget = CommentTarget.FromPost("7491964240782246425");
        Comment sendPostComment = await client.SendCommentAsync(
            postTarget,
            "post cmt"
        );
        await Assert.That(sendPostComment.Id).IsEqualTo("7712448761013797694");

        CommentReply sendPostReply = await client.SendCommentReplyAsync(
            postTarget,
            "7712448761013797694",
            "post reply"
        );
        await Assert.That(sendPostReply.Id).IsEqualTo("7712451462601130776");

        await client.DeleteReplyAsync(postTarget, "7712451462601130776");
        await client.DeleteCommentAsync(postTarget, "7712448761013797694");

        await Assert.That(requests[1].Body.Contains("\"offset\":42121")).IsTrue();
        await Assert.That(requests[1].Body.Contains("\"data_type\":20")).IsTrue();
        await Assert.That(requests[2].Body.Contains("\"group_type\":1")).IsTrue();
        await Assert.That(requests[2].Body.Contains("\"data_type\":4")).IsTrue();
        await Assert.That(requests[3].Body.Contains("\"commit_source\":9")).IsTrue();
        await Assert.That(requests[4].Body.Contains("\"data_type\":9")).IsTrue();
        await Assert.That(requests[4].Body.Contains("\"service_id\":4")).IsTrue();
        await Assert.That(requests[6].Body.Contains("\"group_type\":3")).IsTrue();
        await Assert.That(requests[6].Body.Contains("\"data_type\":11")).IsTrue();
        await Assert.That(requests[7].Body.Contains("\"commit_source\":11")).IsTrue();
        await Assert.That(requests[8].Body.Contains("\"data_type\":9")).IsTrue();
        await Assert.That(requests[8].Body.Contains("\"service_id\":11")).IsTrue();
        await Assert.That(requests[9].Body.Contains("\"data_type\":4")).IsTrue();
        await Assert.That(requests[9].Body.Contains("\"service_id\":11")).IsTrue();
    }

    [Test]
    public async Task ShortVideoAlbumAndPostFavoriteLike_BuildExpectedPayloads()
    {
        const string okJson = """{"code":0,"message":"SUCCESS","data":{}}""";
        List<(string Path, string Body)> requests = [];

        using MockHandler handler = new(
            (req, body) =>
            {
                string path = req.RequestUri?.AbsolutePath ?? string.Empty;
                if (
                    path.Contains("/bookshelf/video/update/v")
                    || path.Contains("/articleapi/do_action/v1/")
                    || path.Contains("/bookshelf/add/v")
                    || path.Contains("/bookshelf/delete/v")
                )
                {
                    requests.Add((path, body ?? string.Empty));
                }
                return okJson;
            }
        );

        using HttpClient httpClient = new(handler);
        using PhoenixReadApiClient client = new(httpClient);

        await client.SetFavoriteAsync(FavoriteTarget.FromShortVideo("7711744740370156569"), true);
        await client.SetFavoriteAsync(FavoriteTarget.FromShortVideo("7711744740370156569"), false);
        await client.SetFavoriteAsync(FavoriteTarget.FromVideoAlbum("7706480483017690137"), true);
        await client.SetFavoriteAsync(FavoriteTarget.FromVideoAlbum("7706480483017690137"), false);
        await client.SetFavoriteAsync(FavoriteTarget.FromPost("7679289401964298776"), true);
        await client.SetFavoriteAsync(FavoriteTarget.FromPost("7679289401964298776"), false);
        await client.SetFavoriteAsync(FavoriteTarget.FromAudiobook("7259706204455373885"), true);
        await client.SetFavoriteAsync(FavoriteTarget.FromAudiobook("7181811308424072249"), false);
        await client.SetFavoriteAsync(FavoriteTarget.FromNovel("7414011485757639704"), true);
        await client.SetFavoriteAsync(FavoriteTarget.FromNovel("7414011485757639704"), false);
        await client.SetShortVideoLikeAsync("7711744740370156569", true);
        await client.SetShortVideoLikeAsync("7711744740370156569", false);
        await client.SetPostLikeAsync("7679289401964298776", true);
        await client.SetPostLikeAsync("7679289401964298776", false);

        await Assert.That(requests[0].Path.Contains("/bookshelf/video/update/v")).IsTrue();
        await Assert.That(requests[0].Body.Contains("\"book_type\":4")).IsTrue();
        await Assert.That(requests[0].Body.Contains("\"video_shelf_operate_type\":0")).IsTrue();
        await Assert.That(requests[1].Body.Contains("\"video_shelf_operate_type\":1")).IsTrue();

        await Assert.That(requests[2].Body.Contains("\"book_type\":3")).IsTrue();
        await Assert.That(requests[2].Body.Contains("\"video_shelf_operate_type\":0")).IsTrue();
        await Assert.That(requests[3].Body.Contains("\"video_shelf_operate_type\":1")).IsTrue();

        await Assert.That(requests[6].Path.Contains("/bookshelf/add/v")).IsTrue();
        await Assert.That(requests[6].Body.Contains("\"book_type\":1")).IsTrue();
        await Assert.That(requests[7].Path.Contains("/bookshelf/delete/v")).IsTrue();
        await Assert.That(requests[7].Body.Contains("\"book_type\":1")).IsTrue();
        await Assert.That(requests[8].Path.Contains("/bookshelf/add/v")).IsTrue();
        await Assert.That(requests[8].Body.Contains("\"book_type\":0")).IsTrue();
        await Assert.That(requests[9].Path.Contains("/bookshelf/delete/v")).IsTrue();

        await Assert.That(requests[4].Path.Contains("/articleapi/do_action/v1/")).IsTrue();
        await Assert.That(requests[4].Body.Contains("\"object_type\":16")).IsTrue();
        await Assert.That(requests[4].Body.Contains("\"action_type\":25")).IsTrue();
        await Assert.That(requests[5].Body.Contains("\"action_type\":26")).IsTrue();

        await Assert.That(requests[10].Body.Contains("\"object_type\":10")).IsTrue();
        await Assert.That(requests[10].Body.Contains("\"action_type\":3")).IsTrue();
        await Assert.That(requests[11].Body.Contains("\"action_type\":4")).IsTrue();

        await Assert.That(requests[12].Body.Contains("\"object_type\":16")).IsTrue();
        await Assert.That(requests[12].Body.Contains("\"action_type\":3")).IsTrue();
        await Assert.That(requests[13].Body.Contains("\"action_type\":4")).IsTrue();
    }

    [Test]
    public async Task GetBookVideoAndPostFavorites_MapExpectedTypes()
    {
        const string bookShelfJson = """
            {
              "code": 0,
              "message": "SUCCESS",
              "data": {
                "book_shelf_info_all": [
                  { "book_id": "7414011485757639704", "book_type": 0, "serial_count": "515", "tags": "历史古代,穿越" },
                  { "book_id": "7181811308424072249", "book_type": 1, "serial_count": "471", "tags": "宫斗宅斗,古代言情" }
                ],
                "limit": 2000
              }
            }
            """;

        const string videoShelfJson = """
            {
              "code": 0,
              "message": "SUCCESS",
              "data": {
                "video_shelf_info": [
                  { "series_id": "7666132887825042494", "book_type": 4 },
                  { "series_id": "7706480483017690137", "book_type": 3 },
                  { "series_id": "7683879553894190105", "book_type": 2 }
                ],
                "max_bookshelf_cnt": 10000
              }
            }
            """;

        const string postFavoriteJson = """
            {
              "code": 0,
              "message": "SUCCESS",
              "data": {
                "topic_booklist": [
                  {
                    "booklist_id": "7712163266184757785",
                    "topic_title": "测试图文标题",
                    "postdata": {
                      "post_id": "7712163266184757785",
                      "title": "测试图文标题",
                      "pure_content": "正文内容",
                      "digg_cnt": 12,
                      "reply_cnt": 3
                    }
                  }
                ],
                "has_more": false,
                "next_offset": 1
              }
            }
            """;

        using MockHandler handler = new(
            (req, _) =>
            {
                string path = req.RequestUri?.AbsolutePath ?? string.Empty;
                if (path.Contains("/bookshelf/list/v"))
                    return bookShelfJson;
                if (path.Contains("/bookshelf/video/list/v"))
                    return videoShelfJson;
                if (path.Contains("/ugc/booklist/get/v"))
                    return postFavoriteJson;
                return "{}";
            }
        );

        using HttpClient httpClient = new(handler);
        using PhoenixReadApiClient client = new(httpClient);

        IReadOnlyList<Novel> novels = await client.GetNovelFavoritesAsync();
        await Assert.That(novels.Count).IsEqualTo(1);
        await Assert.That(novels[0].Id).IsEqualTo("7414011485757639704");
        await Assert.That(novels[0].SerialCount).IsEqualTo(515);

        IReadOnlyList<Audiobook> audiobooks = await client.GetAudiobookFavoritesAsync();
        await Assert.That(audiobooks.Count).IsEqualTo(1);
        await Assert.That(audiobooks[0].Id).IsEqualTo("7181811308424072249");

        IReadOnlyList<ShortVideo> shortVideos = await client.GetShortVideoFavoritesAsync();
        await Assert.That(shortVideos.Count).IsEqualTo(1);
        await Assert.That(shortVideos[0].VideoId).IsEqualTo("7666132887825042494");

        IReadOnlyList<VideoAlbum> albums = await client.GetVideoAlbumFavoritesAsync();
        await Assert.That(albums.Count).IsEqualTo(1);
        await Assert.That(albums[0].Id).IsEqualTo("7706480483017690137");

        IReadOnlyList<Drama> dramas = await client.GetDramaFavoritesAsync();
        await Assert.That(dramas.Count).IsEqualTo(1);
        await Assert.That(dramas[0].Id).IsEqualTo("7683879553894190105");

        PostFavoritePage posts = await client.GetPostFavoritesAsync();
        await Assert.That(posts.Items.Count).IsEqualTo(1);
        await Assert.That(posts.Items[0].Id).IsEqualTo("7712163266184757785");
        await Assert.That(posts.Items[0].Title).IsEqualTo("测试图文标题");
        await Assert.That(posts.NextOffset).IsEqualTo(1);
        await Assert.That(posts.HasMore).IsFalse();
    }

    [Test]
    public async Task DramaRecommendationsGuessYouLikeAndPlaybackEnd_MapExpectedTypes()
    {
        const string planRelatedJson = """
            {
              "code": "0",
              "message": "SUCCESS",
              "session_id": "sess_plan",
              "data": [
                {
                  "cell_id_str": "7511643039589220414",
                  "show_type": 547,
                  "cell_name": "相关作品",
                  "video_data": [
                    { "series_id": "7593750960325790745", "title": "一品布衣2：烽火篇", "episode_cnt": 80 }
                  ],
                  "book_data": [
                    { "book_id": "7111111111111111111", "book_name": "一品布衣原著", "author": "作者A" }
                  ]
                },
                {
                  "cell_id_str": "6975711477277655048",
                  "show_type": 506,
                  "cell_name": "猜你喜欢",
                  "next_offset": 9,
                  "has_more": true,
                  "cell_data": [
                    {
                      "show_type": 557,
                      "post_data": [
                        { "post_id": "7697469523196330520", "title": "双列图文", "pure_content": "图文正文" }
                      ]
                    },
                    {
                      "show_type": 507,
                      "video_data": [
                        { "series_id": "7533152947274796056", "title": "双列短剧A", "episode_cnt": 60 }
                      ]
                    }
                  ]
                }
              ]
            }
            """;

        const string cellChangeJson = """
            {
              "code": 0,
              "message": "SUCCESS",
              "data": {
                "has_more": false,
                "next_offset": 18,
                "session_id": "sess_next",
                "cell_view": {
                  "cell_id_str": "6975711477277655048",
                  "show_type": 506,
                  "cell_name": "猜你喜欢",
                  "cell_data": [
                    {
                      "show_type": 507,
                      "video_data": [
                        { "series_id": "7527615368282508350", "title": "翻页短剧B", "episode_cnt": 50 }
                      ]
                    }
                  ]
                }
              }
            }
            """;

        const string playbackEndJson = """
            {
              "code": "0",
              "message": "SUCCESS",
              "data": [
                {
                  "show_type": 616,
                  "video_data": [
                    { "series_id": "7697682916310010905", "title": "备用联系人（完）", "episode_cnt": 70 }
                  ]
                }
              ]
            }
            """;

        using MockHandler handler = new(
            (req, _) =>
            {
                string path = req.RequestUri?.AbsolutePath ?? string.Empty;
                string query = req.RequestUri?.Query ?? string.Empty;
                if (path.Contains("/bookmall/cell/change/"))
                    return cellChangeJson;
                if (path.Contains("/plan/v") && query.Contains("detail_page_more_related"))
                    return planRelatedJson;
                if (path.Contains("/plan/v") && query.Contains("series_end_"))
                    return playbackEndJson;
                return "{}";
            }
        );

        using HttpClient httpClient = new(handler);
        using PhoenixReadApiClient client = new(httpClient);

        IReadOnlyList<RelatedWorkItem> related = await client.GetDramaRelatedWorksAsync(
            "7527125460430375961"
        );
        await Assert.That(related.Count).IsEqualTo(2);
        await Assert.That((related[0].Value as Drama)?.Id).IsEqualTo("7593750960325790745");
        await Assert.That((related[1].Value as Novel)?.Id).IsEqualTo("7111111111111111111");

        RecommendationFeedPage page1 = await client.GetDramaGuessYouLikeAsync(
            "7527125460430375961"
        );
        await Assert.That(page1.CellId).IsEqualTo("6975711477277655048");
        await Assert.That(page1.NextOffset).IsEqualTo(9);
        await Assert.That(page1.HasMore).IsTrue();
        await Assert.That(page1.Items.Count).IsEqualTo(2);
        await Assert.That((page1.Items[0].Value as Post)?.Id).IsEqualTo("7697469523196330520");
        await Assert.That((page1.Items[1].Value as Drama)?.Id).IsEqualTo("7533152947274796056");

        RecommendationFeedPage page2 = await client.GetDramaGuessYouLikeAsync(
            "7527125460430375961",
            page1.NextOffset,
            page1.CellId,
            page1.SessionId
        );
        await Assert.That(page2.HasMore).IsFalse();
        await Assert.That(page2.NextOffset).IsEqualTo(18);
        await Assert.That((page2.Items[0].Value as Drama)?.Id).IsEqualTo("7527615368282508350");

        Drama? endDrama = await client.GetPlaybackEndRecommendationAsync(
            "7706480483017690137",
            isVideoAlbum: true
        );
        await Assert.That(endDrama?.Id).IsEqualTo("7697682916310010905");
    }

    [Test]
    public async Task HistoryAndProgressReporting_SendExpectedPayloadsAndMapAccumulatedTime()
    {
        List<(string Path, string? Body)> requests = [];
        const string historyListJson = """
            {
              "code": 0,
              "message": "SUCCESS",
              "data": {
                "data_list": [
                  {
                    "id": "h1",
                    "book_id_str": "7707621265934126142",
                    "vid": "7707623348863241278",
                    "vid_index": 4,
                    "title": "测试短剧",
                    "current_play_position": 10168,
                    "player_accumulate_total_time": 143986,
                    "duration": 180000
                  }
                ],
                "total": 1,
                "next_offset": 1,
                "has_more": false
              }
            }
            """;

        using MockHandler handler = new(
            (req, body) =>
            {
                string path = req.RequestUri?.AbsolutePath ?? string.Empty;
                requests.Add((path, body));
                if (path.Contains("/read_history/list/v"))
                {
                    return historyListJson;
                }

                return """{"code":0,"message":"SUCCESS","data":[]}""";
            }
        );

        using HttpClient httpClient = new(handler);
        using PhoenixReadApiClient client = new(httpClient);

        HistoryPage<Drama> history = await client.GetDramaHistoryAsync();
        await Assert.That(history.Items.Count).IsEqualTo(1);
        await Assert.That(history.Items[0].CurrentPositionMs).IsEqualTo(10168);
        await Assert.That(history.Items[0].AccumulatedPlayTimeMs).IsEqualTo(143986);

        await client.ReportHistoryAsync([
            HistoryUpdate.FromDrama("7707621265934126142", "7707623346887740478", 3, 14075, 51375),
            HistoryUpdate.FromVideoAlbum(
                "7706480483017690137",
                "7712411168840633406",
                56,
                8345,
                71381
            ),
            HistoryUpdate.FromShortVideo("7705807590516608024", 100, 5025),
            HistoryUpdate.FromPost("7712163266184757785"),
            HistoryUpdate.FromNovel("7414011485757639704", 2),
            HistoryUpdate.FromAudiobook("7181811308424072249", "7259706204455373885", 1),
        ]);

        await client.ReportProgressAsync([
            ProgressUpdate.FromNovel(
                "7414011485757639704",
                "7414011725348864536",
                pageIndex: 7,
                pageProgressRate: 0.0011f,
                chapterProgressRate: "26.1334",
                bookProgressRate: "0.0019",
                paragraphId: "44",
                paragraphOffset: 0,
                position: new ReadingPosition
                {
                    StartContainerIndex = 44,
                    StartElementIndex = 0,
                    StartElementOffset = 0,
                }
            ),
            ProgressUpdate.FromAudiobook(
                "7259706204455373885",
                "7283088135259491390",
                3200,
                "0.003",
                "0"
            ),
            ProgressUpdate.FromDrama("7707621265934126142", "7707623348863241278", 10168, 143986),
            ProgressUpdate.FromVideoAlbum(
                "7706480483017690137",
                "7712411168714804286",
                5441,
                685625
            ),
            ProgressUpdate.FromShortVideo("7705807590516608024", 100, 5025),
        ]);

        await Assert
            .That(async () =>
            {
                await client.ReportProgressAsync(
                    new ProgressUpdate
                    {
                        ContentType = HistoryContentType.Post,
                        TargetId = "7712163266184757785",
                    }
                );
            })
            .Throws<ArgumentOutOfRangeException>();

        (string Path, string? Body) historyReq = requests.Single(r =>
            r.Path.EndsWith("/read_history/update/v", StringComparison.Ordinal)
        );
        await Assert.That(historyReq.Body).Contains("\"book_type\":3");
        await Assert.That(historyReq.Body).Contains("\"listen_book_id\":\"7259706204455373885\"");
        await Assert.That(historyReq.Body).Contains("\"chapter_index\":2");

        (string Path, string? Body) progressReq = requests.Single(r =>
            r.Path.EndsWith("/read_progress/upload/v", StringComparison.Ordinal)
        );
        await Assert.That(progressReq.Body).Contains("\"books\":[");
        await Assert.That(progressReq.Body).Contains("\"start_container_index\":44");
        await Assert.That(progressReq.Body).Contains("\"player_cumulative_total_duration\":143986");
    }

    private sealed class MockHandler(Func<HttpRequestMessage, string?, string> responder)
        : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            string? body = request.Content is not null
                ? await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false)
                : null;
            string json = responder(request, body);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };
        }
    }
}
