namespace SecureOtpVerificationService;
public sealed class ConsoleOtpInput : IOtpInput
{
    public async Task<string?> ReadOtpAsync(CancellationToken cancellationToken)
    {
        Console.Write("Enter 6-digit OTP: ");
        return await Console.In.ReadLineAsync(cancellationToken);
    }
}
