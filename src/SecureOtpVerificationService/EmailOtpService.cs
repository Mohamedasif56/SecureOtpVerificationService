using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;

namespace SecureOtpVerificationService;

public sealed class EmailOtpService : IDisposable
{
    private const string AllowedDomain = "dso.org.sg";
    private const int MaximumAttempts = 10;

    private readonly IEmailSender _emailSender;
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _otpLifetime;
    private readonly SemaphoreSlim _stateLock = new(1, 1);

    private string? _currentOtp;
    private DateTimeOffset? _expiresAt;
    private bool _disposed;

    public EmailOtpService(IEmailSender emailSender,
        TimeProvider? timeProvider = null, TimeSpan? otpLifetime = null)
    {
        _emailSender = emailSender ?? throw new ArgumentNullException(nameof(emailSender));
        _timeProvider = timeProvider ?? TimeProvider.System;
        _otpLifetime = otpLifetime ?? TimeSpan.FromMinutes(1);

        if (_otpLifetime <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(otpLifetime));
    }

    public void Start() => ObjectDisposedException.ThrowIf(_disposed, this);

    public async Task<OtpStatus> GenerateOtpEmailAsync(string userEmail,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!IsValidAllowedEmail(userEmail))
            return OtpStatus.STATUS_EMAIL_INVALID;

        var otp = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        var expiresAt = _timeProvider.GetUtcNow().Add(_otpLifetime);
        var emailBody = $"Your OTP Code is {otp}. The code is valid for 1 minute";

        try
        {
            if (!await _emailSender.SendEmailAsync(userEmail, emailBody, cancellationToken))
                return OtpStatus.STATUS_EMAIL_FAIL;

            await _stateLock.WaitAsync(cancellationToken);
            try
            {
                _currentOtp = otp;
                _expiresAt = expiresAt;
            }
            finally { _stateLock.Release(); }

            return OtpStatus.STATUS_EMAIL_OK;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return OtpStatus.STATUS_EMAIL_FAIL;
        }
    }

    public async Task<OtpStatus> CheckOtpAsync(IOtpInput input,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(input);

        string? expectedOtp;
        DateTimeOffset? expiresAt;

        await _stateLock.WaitAsync(cancellationToken);
        try
        {
            expectedOtp = _currentOtp;
            expiresAt = _expiresAt;
        }
        finally { _stateLock.Release(); }

        if (expectedOtp is null || expiresAt is null)
            return OtpStatus.STATUS_OTP_TIMEOUT;

        for (var attempt = 1; attempt <= MaximumAttempts; attempt++)
        {
            var remaining = expiresAt.Value - _timeProvider.GetUtcNow();
            if (remaining <= TimeSpan.Zero)
            {
                await ClearOtpAsync();
                return OtpStatus.STATUS_OTP_TIMEOUT;
            }

            using var timeoutCts =
                CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(remaining);

            string? enteredOtp;
            try
            {
                enteredOtp = await input.ReadOtpAsync(timeoutCts.Token);
            }
            catch (OperationCanceledException) when (
                !cancellationToken.IsCancellationRequested &&
                timeoutCts.IsCancellationRequested)
            {
                await ClearOtpAsync();
                return OtpStatus.STATUS_OTP_TIMEOUT;
            }

            // Critical expiry re-check after a blocking input returns.
            if (_timeProvider.GetUtcNow() >= expiresAt.Value)
            {
                await ClearOtpAsync();
                return OtpStatus.STATUS_OTP_TIMEOUT;
            }

            if (IsSixDigitOtp(enteredOtp) &&
                CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(enteredOtp!),
                    Encoding.UTF8.GetBytes(expectedOtp)))
            {
                await ClearOtpAsync();
                return OtpStatus.STATUS_OTP_OK;
            }
        }

        await ClearOtpAsync();
        return OtpStatus.STATUS_OTP_FAIL;
    }

    public void Close() => Dispose();

    public void Dispose()
    {
        if (_disposed) return;
        _currentOtp = null;
        _expiresAt = null;
        _stateLock.Dispose();
        _disposed = true;
    }

    private static bool IsValidAllowedEmail(string? userEmail)
    {
        if (string.IsNullOrWhiteSpace(userEmail)) return false;
        try
        {
            var address = new MailAddress(userEmail);
            return string.Equals(address.Address, userEmail, StringComparison.OrdinalIgnoreCase)
                && string.Equals(address.Host, AllowedDomain, StringComparison.OrdinalIgnoreCase);
        }
        catch (FormatException) { return false; }
    }

    private static bool IsSixDigitOtp(string? otp) =>
        otp is { Length: 6 } && otp.All(char.IsAsciiDigit);

    private async Task ClearOtpAsync()
    {
        await _stateLock.WaitAsync();
        try
        {
            _currentOtp = null;
            _expiresAt = null;
        }
        finally { _stateLock.Release(); }
    }
}
