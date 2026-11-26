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

        using MockHandler handler = new((req, _) =>
        {
            string path = req.RequestUri?.AbsolutePath ?? string.Empty;
            return path.Contains("/search/tab/v") ? searchJson : "{}";
        });

        using HttpClient httpClient = new(handler);
        using PhoenixReadApiClient client = new(httpClient);

        SearchPage<User> result = await client.SearchUsersAsync("刘念");

        await Assert.That(result.Items.Count).IsEqualTo(1);
        await Assert.That(result.Items[0].Id).IsEqualTo("3112051666323866");
        await Assert.That(result.Items[0].Name).IsEqualTo("刘念");
        await Assert.That(result.Items[0].AvatarUrl).IsEqualTo("https://example.com/avatar-liunian.jpg");
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

        using MockHandler handler = new((req, _) =>
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
        });

        using HttpClient httpClient = new(handler);
        using PhoenixReadApiClient client = new(httpClient);

        SearchPage<VideoSearchItem> videos = await client.SearchVideosAsync("娇女诡爱畸恋");
        await Assert.That(videos.Items.Count).IsEqualTo(2);

        DramaAlbum? albumHit = videos.Items[0].Value as DramaAlbum;
        ShortVideo? shortHit = videos.Items[1].Value as ShortVideo;
        await Assert.That(albumHit).IsNotNull();
        await Assert.That(shortHit).IsNotNull();
        await Assert.That(albumHit!.Id).IsEqualTo("7706480483017690137");
        await Assert.That(shortHit!.VideoId).IsEqualTo("7689008778074655806");
        await Assert.That(shortHit.DurationSeconds).IsEqualTo(189);

        DramaAlbum album = await client.GetAlbumAsync(albumHit.Id);
        await Assert.That(album.Id).IsEqualTo("7706480483017690137");
        await Assert.That(album.Seasons.Count).IsEqualTo(1);
        await Assert.That(album.Seasons[0].DramaId).IsEqualTo("7706480472527752216");
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

        using MockHandler handler = new((req, _) =>
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
        });

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

        using MockHandler handler = new((req, body) =>
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
        });

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

        const string deleteSuccessJson = """{"code":0,"message":"SUCCESS","data":{"update_fail_datas":[]}}""";

        string? capturedDeleteBody = null;

        using MockHandler handler = new((req, body) =>
        {
            string path = req.RequestUri?.AbsolutePath ?? string.Empty;
            string query = req.RequestUri?.Query ?? string.Empty;
            if (path.Contains("/read_history/list/v"))
            {
                if (query.Contains("book_type=0")) return novelHistoryJson;
                if (query.Contains("book_type=1")) return audioHistoryJson;
            }
            if (path.Contains("/read_history/update/v"))
            {
                capturedDeleteBody = body;
                return deleteSuccessJson;
            }
            return "{}";
        });

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
        await Assert.That(capturedDeleteBody.Contains("\"listen_book_id\":\"7450063007658298393\"")).IsTrue();
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

        const string addCommentJson = """{"code":0,"message":"success","data":{"comment_info":{"comment_id":"7712448761013797694"}}}""";
        const string addReplyJson = """{"code":0,"message":"success","data":{"reply":{"reply_id":"7712451462601130776"}}}""";
        const string okJson = """{"code":0,"message":"success","data":{}}""";

        List<(string Path, string Body)> requests = [];

        using MockHandler handler = new((req, body) =>
        {
            string path = req.RequestUri?.AbsolutePath ?? string.Empty;
            requests.Add((path, body ?? string.Empty));
            if (path.Contains("/comment/list/")) return danmakuListJson;
            if (path.Contains("/comment/add/")) return addCommentJson;
            if (path.Contains("/reply/add/")) return addReplyJson;
            return okJson;
        });

        using HttpClient httpClient = new(handler);
        using PhoenixReadApiClient client = new(httpClient);

        DanmakuPage dmPage = await client.GetShortVideoDanmakuAsync("7706480472527752216", 0, 60000);
        await Assert.That(dmPage.Items.Count).IsEqualTo(1);
        await Assert.That(dmPage.Items[0].Id).IsEqualTo("7712450675655525144");
        await Assert.That(dmPage.Items[0].OffsetMs).IsEqualTo(42121);

        PhoenixOperationResult sendDm = await client.SendShortVideoDanmakuAsync("7706480472527752216", "test dm", 42121);
        await Assert.That(sendDm.Succeeded).IsTrue();
        await Assert.That(sendDm.Id).IsEqualTo("7712448761013797694");

        PhoenixOperationResult sendSvComment = await client.SendShortVideoCommentAsync("7706480472527752216", "sv cmt");
        await Assert.That(sendSvComment.Id).IsEqualTo("7712448761013797694");

        PhoenixOperationResult sendSvReply = await client.SendShortVideoReplyAsync("7712448761013797694", "7706480472527752216", "sv reply");
        await Assert.That(sendSvReply.Id).IsEqualTo("7712451462601130776");

        await client.DeleteShortVideoReplyAsync("7712451462601130776");
        await client.DeleteShortVideoCommentAsync("7712448761013797694", "7706480472527752216");

        PhoenixOperationResult sendPostComment = await client.SendPostCommentAsync("7491964240782246425", "post cmt");
        await Assert.That(sendPostComment.Id).IsEqualTo("7712448761013797694");

        PhoenixOperationResult sendPostReply = await client.SendPostReplyAsync("7712448761013797694", "7491964240782246425", "post reply");
        await Assert.That(sendPostReply.Id).IsEqualTo("7712451462601130776");

        await client.DeletePostReplyAsync("7712451462601130776");
        await client.DeletePostCommentAsync("7712448761013797694", "7491964240782246425");

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
