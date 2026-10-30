using PhoenixReadApi.Tests.Common;

namespace PhoenixReadApi.Tests;

/// <summary>
/// 账号认证、扫码登录会话与状态校验相关测试。
/// </summary>
public sealed class AuthenticationTests
{
    [Test]
    public async Task TestGetAuthenticationState_Initial()
    {
        AuthenticationState state = SharedScenario.Client.GetAuthenticationState();

        await Assert.That(state).IsNotNull();
        await Assert.That(state.IsAuthenticated).IsFalse();
        await Assert.That(state.User).IsNotNull();
        await Assert.That(state.User.Id).IsEmpty();
        await Assert.That(state.User.Name).IsNotNull();
        await Assert.That(state.User.AvatarUrl).IsNotNull();
        await Assert.That(state.User.Description).IsNotNull();
        await Assert.That(state.User.MaskedPhone).IsNotNull();
    }

    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestValidateSessionAsync()
    {
        using CancellationTokenSource timeout = SharedScenario.CreateTimeout();

        // 匿名状态下主动校验会话有效性应返回 false
        bool isValid = await SharedScenario.Client.ValidateSessionAsync(timeout.Token);
        await Assert.That(isValid).IsFalse();
    }

    [Test, NotInParallel("phoenix-read-api")]
    public async Task TestQrCodeLoginFlow()
    {
        using CancellationTokenSource timeout = SharedScenario.CreateTimeout();

        // 1. 创建扫码登录会话
        QrCodeLoginSession qrSession = await SharedScenario.Client.CreateQrCodeLoginAsync(timeout.Token);

        await Assert.That(qrSession).IsNotNull();
        await Assert.That(string.IsNullOrWhiteSpace(qrSession.Token)).IsFalse();
        await Assert.That(string.IsNullOrWhiteSpace(qrSession.QrCodeBase64)).IsFalse();
        await Assert.That(qrSession.QrCodeImageBytes).IsNotNull();
        await Assert.That(qrSession.QrCodeImageBytes.Length > 0).IsTrue();
        await Assert.That(qrSession.ExpiresInSeconds > 0).IsTrue();

        // 2. 检查扫码登录状态
        QrCodeCheckResult statusResult = await SharedScenario.Client.CheckQrCodeStatusAsync(
            qrSession.Token,
            timeout.Token
        );

        await Assert.That(statusResult).IsNotNull();
        await Assert.That(Enum.IsDefined(typeof(QrCodeLoginStatus), statusResult.Status)).IsTrue();
        await Assert.That(
            statusResult.Status == QrCodeLoginStatus.WaitingForScan
            || statusResult.Status == QrCodeLoginStatus.Scanned
            || statusResult.Status == QrCodeLoginStatus.Expired
        ).IsTrue();
    }

    [Test]
    public async Task TestLogout_SafeMultipleCalls()
    {
        SharedScenario.Client.Logout();
        SharedScenario.Client.Logout();

        AuthenticationState state = SharedScenario.Client.GetAuthenticationState();
        await Assert.That(state.IsAuthenticated).IsFalse();
    }

    [Test]
    public async Task TestAuthenticationValidation_Arguments()
    {
        // 空手机号验证码发送
        await Assert.That(async () =>
        {
            await SharedScenario.Client.SendVerificationCodeAsync("");
        }).Throws<ArgumentException>();

        // 空手机号登录
        await Assert.That(async () =>
        {
            await SharedScenario.Client.LoginAsync("", "1234");
        }).Throws<ArgumentException>();

        // 空验证码登录
        await Assert.That(async () =>
        {
            await SharedScenario.Client.LoginAsync("13800000000", "");
        }).Throws<ArgumentException>();

        // 空 Token 扫码检查
        await Assert.That(async () =>
        {
            await SharedScenario.Client.CheckQrCodeStatusAsync("");
        }).Throws<ArgumentException>();
    }
}
