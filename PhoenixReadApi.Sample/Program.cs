using PhoenixReadApi;
using PhoenixReadApi.Sample;

const string keyword = "请君入我怀";
string sampleDirectory = SampleContext.GetSampleDirectory();
string sessionPath = Path.Combine(sampleDirectory, ".auth", "phone-session.json");
Directory.CreateDirectory(Path.GetDirectoryName(sessionPath)!);
string phone = SampleContext.ReadPhone(sampleDirectory);

using CancellationTokenSource timeout = new(TimeSpan.FromMinutes(15));
using PhoenixReadApiClient client = new(
    new PhoenixReadApiOptions
    {
        MaxPagesPerCategory = 1,
        RequestTimeout = TimeSpan.FromSeconds(25),
        RetryCount = 1,
        CompleteSearchSeasons = false,
        AuthenticationStateFilePath = sessionPath,
    }
);

Console.WriteLine($"关键词：{keyword}");
Console.WriteLine($"会话文件：{sessionPath}");

SampleContext context = new()
{
    Client = client,
    Keyword = keyword,
    CancellationToken = timeout.Token,
};

// 第 1 步：登录（会话校验 → 短信登录 → 二维码登录演示）
await SampleSteps.LoginAsync(context, phone);

// 第 2 步：基础只读接口（不依赖前置结果）
await SampleSteps.RunBasicReadOnlyAsync(context);

// 第 3 步：剧集详情派生接口（依赖搜索/剧库结果）
await SampleSteps.RunDramaDerivedAsync(context);

// 第 4 步：用户相关接口
await SampleSteps.RunUserRelatedAsync(context);

// 第 5 步：分享链接
await SampleSteps.RunShareAsync(context);

// 第 6 步：写入接口（需要登录）
if (context.Authentication.IsAuthenticated)
{
    await SampleSteps.RunWriteOperationsAsync(context);
}
else
{
    Console.WriteLine("未登录，跳过需要账号权限的写入接口。");
}

// 第 7 步：场景功能演示（对应红果 App 三大特色页面：筛选找剧、排行榜、上新日历）
await SampleSteps.RunFeatureDemosAsync(context);

Console.WriteLine("样例执行完成。会话文件按要求保留，未调用 Logout()。");
