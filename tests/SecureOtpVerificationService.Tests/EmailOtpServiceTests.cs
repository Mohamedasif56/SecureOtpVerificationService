using System.Text.RegularExpressions;
using Xunit;

namespace SecureOtpVerificationService.Tests;

public sealed class EmailOtpServiceTests
{
    [Theory]
    [InlineData("")]
    [InlineData("invalid-email")]
    [InlineData("user@gmail.com")]
    [InlineData("user@fakedso.org.sg")]
    [InlineData("user@dso.org.sg.fake.com")]
    public async Task GenerateOtpEmailAsync_InvalidEmail_ReturnsInvalid(string email)
    {
        var sender = new FakeEmailSender();
        using var sut = new EmailOtpService(sender);
        var result = await sut.GenerateOtpEmailAsync(email);
        Assert.Equal(OtpStatus.STATUS_EMAIL_INVALID, result);
    }

    [Fact]
    public async Task GenerateOtpEmailAsync_ValidEmail_ReturnsOk()
    {
        var sender = new FakeEmailSender();
        using var sut = new EmailOtpService(sender);
        var result = await sut.GenerateOtpEmailAsync("user@dso.org.sg");
        Assert.Equal(OtpStatus.STATUS_EMAIL_OK, result);
        Assert.Matches(@"^You OTP Code is \d{6}\. The code is valid for 1 minute$", sender.LastBody!);
    }

    [Fact]
    public async Task GenerateOtpEmailAsync_EmailFailure_ReturnsFail()
    {
        var sender = new FakeEmailSender { ShouldSucceed = false };
        using var sut = new EmailOtpService(sender);
        var result = await sut.GenerateOtpEmailAsync("user@dso.org.sg");
        Assert.Equal(OtpStatus.STATUS_EMAIL_FAIL, result);
    }

    [Fact]
    public async Task CheckOtpAsync_CorrectOtp_ReturnsOk()
    {
        var sender = new FakeEmailSender();
        using var sut = new EmailOtpService(sender);
        await sut.GenerateOtpEmailAsync("user@dso.org.sg");
        var otp = ExtractOtp(sender.LastBody!);
        var result = await sut.CheckOtpAsync(new FakeOtpInput(otp));
        Assert.Equal(OtpStatus.STATUS_OTP_OK, result);
    }

    [Fact]
    public async Task CheckOtpAsync_TenWrongAttempts_ReturnsFail()
    {
        var sender = new FakeEmailSender();
        using var sut = new EmailOtpService(sender);
        await sut.GenerateOtpEmailAsync("user@dso.org.sg");
        var result = await sut.CheckOtpAsync(
            new FakeOtpInput(Enumerable.Repeat("000000", 10).ToArray()));
        Assert.Equal(OtpStatus.STATUS_OTP_FAIL, result);
    }

    [Fact]
    public async Task CheckOtpAsync_BlockingInput_ReturnsTimeout()
    {
        var sender = new FakeEmailSender();
        using var sut = new EmailOtpService(sender, otpLifetime: TimeSpan.FromMilliseconds(50));
        await sut.GenerateOtpEmailAsync("user@dso.org.sg");
        var result = await sut.CheckOtpAsync(new BlockingOtpInput());
        Assert.Equal(OtpStatus.STATUS_OTP_TIMEOUT, result);
    }

    [Fact]
    public async Task CheckOtpAsync_CorrectOtpEnteredAfterExpiry_ReturnsTimeout()
    {
        var sender = new FakeEmailSender();
        using var sut = new EmailOtpService(sender, otpLifetime: TimeSpan.FromMilliseconds(100));
        await sut.GenerateOtpEmailAsync("user@dso.org.sg");
        var otp = ExtractOtp(sender.LastBody!);

        await Task.Delay(150);

        var result = await sut.CheckOtpAsync(new FakeOtpInput(otp));
        Assert.Equal(OtpStatus.STATUS_OTP_TIMEOUT, result);
    }

    private static string ExtractOtp(string body) => Regex.Match(body, @"\d{6}").Value;

    private sealed class FakeEmailSender : IEmailSender
    {
        public bool ShouldSucceed { get; init; } = true;
        public string? LastBody { get; private set; }

        public Task<bool> SendEmailAsync(string emailAddress, string emailBody,
            CancellationToken cancellationToken = default)
        {
            LastBody = emailBody;
            return Task.FromResult(ShouldSucceed);
        }
    }

    private sealed class FakeOtpInput(params string[] values) : IOtpInput
    {
        private readonly Queue<string> _values = new(values);
        public Task<string?> ReadOtpAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<string?>(_values.Count > 0 ? _values.Dequeue() : "000000");
        }
    }

    private sealed class BlockingOtpInput : IOtpInput
    {
        public async Task<string?> ReadOtpAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return null;
        }
    }
}
