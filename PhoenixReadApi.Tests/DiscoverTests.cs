using System.Net;
using System.Text;
using PhoenixReadApi.Tests.Common;

namespace PhoenixReadApi.Tests;

/// <summary>
/// 发现页 (GetDiscoverPageAsync) 协议解析、字段映射、游标续传与性能优化的确定性测试。
/// </summary>
public sealed class DiscoverTests
{
    [Test]
    public async Task TestDiscoverPage_MapsNestedVideoDetailAndDirectCellDetail_WithoutEagerChangeRequest()
    {
        const string tabJson = """
            {
              "code": "0",
              "message": "success",
              "data": {
                "tab_item": [
                  {
                    "tab_type": 16,
                    "title": "推荐",
                    "session_id": "session-rec-1",
                    "client_template": "7",
                    "cell_data": [
                      {
                        "cell_id_str": "7294257141911650341",
                        "show_type": 407,
                        "cell_name": "猜你喜欢",
                        "cell_data": [
                          {
                            "show_type": 407,
                            "video_detail": {
                              "series_title": "合集外层标题",
                              "video_detail_list": [
                                {
                                  "series_id": 7709153171259542553,
                                  "series_id_str": "7709153171259542553",
                                  "series_title": "重生高三，请停止你的魅力输出",
                                  "series_intro": "高三逆袭故事简介",
                                  "series_cover": "https://example.com/cover1.jpg",
                                  "episode_cnt": 150,
                                  "episode_right_text": "共150集",
                                  "series_play_cnt": 1381055,
                                  "followed_cnt": 577833,
                                  "series_status": 1,
                                  "status": 1,
                                  "is_multi_season": true,
                                  "related_album_id": 7710935414118304830,
                                  "category_schema": "[{\"name\":\"成长\"},{\"name\":\"重生逆袭\"}]",
                                  "rec_tags": [
                                    { "content": "1345万热搜", "rec_type": 9 }
                                  ]
                                }
                              ]
                            }
                          },
                          {
                            "show_type": 407,
                            "video_data": [
                              {
                                "vid": "",
                                "cover": "",
                                "duration": 0,
                                "series_id": "7704577081954274328",
                                "create_time": "1793861641",
                                "season_index": 3,
                                "rec_tags": [
                                  { "content": "臻果月榜 No.2", "rec_type": 9 },
                                  { "content": "398万热搜", "rec_type": 9 }
                                ],
                                "video_detail": {
                                  "series_id": 7704577081954274328,
                                  "series_id_str": "7704577081954274328",
                                  "series_title": "母凭子贵后被皇家宠上天3",
                                  "series_intro": "古装甜宠剧情简介",
                                  "series_cover": "https://example.com/cover2.jpg",
                                  "episode_cnt": 71,
                                  "episode_right_text": "共71集",
                                  "series_play_cnt": 31807046,
                                  "followed_cnt": 846808,
                                  "series_status": 1,
                                  "status": 1,
                                  "is_multi_season": true,
                                  "related_album_id": 7634062057746254873,
                                  "category_schema": "[{\"name\":\"爱情\"},{\"name\":\"古风爱情\"}]",
                                  "abstract_tags": [
                                    { "content": "甜宠", "data_type": 3 }
                                  ],
                                  "secondary_infos": [
                                    { "content": "9.3分", "data_type": 4 },
                                    { "content": "第3季", "data_type": 43 },
                                    { "content": "打脸反派", "data_type": 3 }
                                  ],
                                  "celebrities": [
                                    {
                                      "celebrity_id": "7521654909997962558",
                                      "nickname": "陈杨婷丹",
                                      "role_name": "黛蜜梨",
                                      "avatar": "https://example.com/avatar1.jpg"
                                    }
                                  ]
                                }
                              }
                            ]
                          },
                          {
                            "show_type": 407,
                            "video_data": [
                              {
                                "series_id": "7705454801290546200",
                                "video_detail": {
                                  "series_id": 7705454801290546200,
                                  "series_title": "单季剧集测试",
                                  "series_cover": "https://example.com/cover3.jpg",
                                  "episode_cnt": 80,
                                  "related_album_id": 0,
                                  "is_multi_season": false
                                }
                              }
                            ]
                          }
                        ]
                      }
                    ]
                  }
                ]
              }
            }
            """;

        RecordingHandler handler = new(uri =>
            uri.AbsolutePath.Contains("/reading/bookapi/bookmall/tab/v", StringComparison.Ordinal)
                ? tabJson
                : throw new InvalidOperationException($"Unexpected request: {uri}")
        );

        using HttpClient httpClient = new(handler);
        using PhoenixReadApiClient client = new(httpClient);

        DiscoverPage page = await client.GetDiscoverPageAsync(DiscoverChannel.Recommend);

        await Assert.That(handler.Requests.Count).IsEqualTo(1);
        await Assert.That(page.HasMore).IsTrue();
        await Assert.That(string.IsNullOrWhiteSpace(page.Cursor)).IsFalse();
        await Assert.That(page.Dramas.Count).IsEqualTo(3);

        Drama first = page.Dramas[0];
        await SharedScenario.AssertValidDrama(first);
        await Assert.That(first.Id).IsEqualTo("7709153171259542553");
        await Assert.That(first.Title).IsEqualTo("重生高三，请停止你的魅力输出");
        await Assert.That(first.Description).IsEqualTo("高三逆袭故事简介");
        await Assert.That(first.CoverUrl).IsEqualTo("https://example.com/cover1.jpg");
        await Assert.That(first.EpisodeCount).IsEqualTo(150);
        await Assert.That(first.Remark).IsEqualTo("共150集");
        await Assert.That(first.PlayCount).IsEqualTo(1381055);
        await Assert.That(first.FollowCount).IsEqualTo(577833);
        await Assert.That(first.Heat).IsEqualTo("1345万热搜");
        await Assert.That(first.ReleaseStatus).IsEqualTo(DramaReleaseStatus.Finished);

        Drama second = page.Dramas[1];
        await SharedScenario.AssertValidDrama(second);
        await Assert.That(second.Score).IsEqualTo("9.3");
        await Assert.That(second.Heat).IsEqualTo("398万热搜");
        await Assert.That(second.OnlineDate).IsEqualTo("2026-11-05");
        await Assert.That(second.Credits.Count).IsEqualTo(1);
        await Assert.That(second.Credits[0].User.Name).IsEqualTo("陈杨婷丹");
        await Assert.That(second.Tags.Contains("甜宠")).IsTrue();
        await Assert.That(second.Tags.Contains("打脸反派")).IsTrue();

        Drama third = page.Dramas[2];
        await SharedScenario.AssertValidDrama(third);
    }

    [Test]
    public async Task TestDiscoverPage_ExploreMapsFlatFieldsAndCursorUsesCellNextOffset()
    {
        const string tabJson = """
            {
              "code": "0",
              "data": {
                "tab_item": [
                  {
                    "tab_type": 8,
                    "title": "找剧",
                    "session_id": "session-exp-1",
                    "client_template": "12",
                    "next_offset": 1,
                    "cell_data": [
                      {
                        "cell_id_str": "6952850996422770718",
                        "show_type": 506,
                        "next_offset": 6,
                        "cell_data": [
                          {
                            "show_type": 507,
                            "video_data": [
                              {
                                "series_id": "7671966197985856536",
                                "title": "持械入宋：第一季",
                                "video_desc": "宋末年故事",
                                "cover": "https://example.com/explore1.jpg",
                                "play_cnt": 22694041,
                                "rec_text": "5669万热度",
                                "score": "8.2",
                                "episode_cnt": 100,
                                "sub_title_list": [
                                  { "content": "古装", "data_type": 3 },
                                  { "content": "穿越", "data_type": 3 }
                                ],
                                "tag_info": { "text": "爆剧" }
                              }
                            ]
                          }
                        ]
                      }
                    ]
                  }
                ]
              }
            }
            """;

        const string changeJson = """
            {
              "code": "0",
              "data": {
                "has_more": true,
                "next_offset": 12,
                "session_id": "session-exp-2",
                "cell_view": {
                  "cell_data": [
                    {
                      "show_type": 507,
                      "video_data": [
                        {
                          "series_id": "7668301750553545752",
                          "title": "我本神明",
                          "video_desc": "都市玄幻",
                          "cover": "https://example.com/explore2.jpg",
                          "play_cnt": 986048,
                          "rec_text": "4425万热度",
                          "score": "8.1",
                          "episode_cnt": 70,
                          "tag_info": { "text": "预约上新" }
                        }
                      ]
                    }
                  ]
                }
              }
            }
            """;

        RecordingHandler handler = new(uri =>
        {
            if (uri.AbsolutePath.Contains("/reading/bookapi/bookmall/tab/v", StringComparison.Ordinal))
            {
                return tabJson;
            }
            if (uri.AbsolutePath.Contains("/reading/bookapi/bookmall/cell/change/v", StringComparison.Ordinal))
            {
                return changeJson;
            }
            if (uri.AbsolutePath.Contains("/service/2/device_register/", StringComparison.Ordinal))
            {
                return "{\"device_id\":1589262967141403,\"install_id\":2380912043756905}";
            }
            return "{}";
        });

        using HttpClient httpClient = new(handler);
        using PhoenixReadApiClient client = new(
            httpClient,
            new PhoenixReadApiOptions
            {
                DeviceId = "1589262967141403",
                InstallId = "2380912043756905",
            }
        );

        DiscoverPage page1 = await client.GetDiscoverPageAsync(DiscoverChannel.Explore);
        await Assert.That(handler.Requests.Count).IsEqualTo(1);
        await Assert.That(page1.Dramas.Count).IsEqualTo(1);
        await Assert.That(page1.Dramas[0].Heat).IsEqualTo("5669万热度");
        await Assert.That(page1.Dramas[0].Score).IsEqualTo("8.2");
        await Assert.That(page1.Dramas[0].Tags.Contains("古装")).IsTrue();

        DiscoverPage page2 = await client.GetDiscoverPageAsync(DiscoverChannel.Explore, page1.Cursor);
        string? changeRequestQuery = handler.Requests
            .Last(r => r.AbsolutePath.Contains("/bookmall/cell/change/v", StringComparison.Ordinal))
            .Query;
        await Assert.That(changeRequestQuery.Contains("offset=6", StringComparison.Ordinal)).IsTrue();
        await Assert.That(page2.Dramas.Count).IsEqualTo(1);
        await Assert.That(page2.Dramas[0].Id).IsEqualTo("7668301750553545752");
        await Assert.That(page2.Dramas[0].ReleaseStatus).IsEqualTo(DramaReleaseStatus.Upcoming);
    }

    [Test]
    public async Task TestDiscoverPage_FallsBackToCellChangeWhenTabHasNoItems_AndMapsNovels()
    {
        const string emptyTabJson = """
            {
              "code": "0",
              "data": {
                "tab_item": [
                  {
                    "tab_type": 2,
                    "title": "小说",
                    "session_id": "session-novel-1",
                    "client_template": "0",
                    "cell_data": [
                      {
                        "cell_id_str": "7098235271900037133",
                        "show_type": 355,
                        "cell_data": []
                      }
                    ]
                  }
                ]
              }
            }
            """;

        const string changeJson = """
            {
              "code": "0",
              "data": {
                "has_more": true,
                "next_offset": 20,
                "session_id": "session-novel-2",
                "cell_view": {
                  "book_data": [
                    {
                      "book_id": "7297873858458029107",
                      "book_name": "测试小说",
                      "book_short_name": "测小",
                      "author": "测试作者",
                      "abstract": "小说简介",
                      "thumb_url": "https://example.com/novel.jpg",
                      "serial_count": 738,
                      "read_count": 31506,
                      "creation_status": "0",
                      "tags": "宫斗宅斗,重生",
                      "category_schema": "[{\"name\":\"古代言情\"}]"
                    }
                  ]
                }
              }
            }
            """;

        RecordingHandler handler = new(uri =>
        {
            if (uri.AbsolutePath.Contains("/reading/bookapi/bookmall/tab/v", StringComparison.Ordinal))
            {
                return emptyTabJson;
            }
            if (uri.AbsolutePath.Contains("/reading/bookapi/bookmall/cell/change/v", StringComparison.Ordinal))
            {
                return changeJson;
            }
            return "{}";
        });

        using HttpClient httpClient = new(handler);
        using PhoenixReadApiClient client = new(httpClient);

        DiscoverPage page = await client.GetDiscoverPageAsync(DiscoverChannel.Novel);

        await Assert.That(page.Novels.Count).IsEqualTo(1);
        Novel novel = page.Novels[0];
        await Assert.That(novel.Id).IsEqualTo("7297873858458029107");
        await Assert.That(novel.ShortTitle).IsEqualTo("测小");
        await Assert.That(novel.Status).IsEqualTo(NovelCreationStatus.Completed);
        await Assert.That(novel.StatusText).IsEqualTo("已完结");
        await Assert.That(novel.Tags.Contains("宫斗宅斗")).IsTrue();
        await Assert.That(novel.Tags.Contains("古代言情")).IsTrue();
        await Assert.That(page.HasMore).IsTrue();
        await Assert.That(string.IsNullOrWhiteSpace(page.Cursor)).IsFalse();
    }

    private sealed class RecordingHandler(Func<Uri, string> responder) : HttpMessageHandler
    {
        public List<Uri> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            Uri uri = request.RequestUri!;
            Requests.Add(uri);
            string json = responder(uri);
            HttpResponseMessage response = new(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };
            return Task.FromResult(response);
        }
    }
}
