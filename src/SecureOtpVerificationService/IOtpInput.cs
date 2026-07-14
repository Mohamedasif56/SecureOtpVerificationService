namespace SecureOtpVerificationService;
public interface IOtpInput
{
    Task<string?> ReadOtpAsync(CancellationToken cancellationToken);
}
