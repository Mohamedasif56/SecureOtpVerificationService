namespace SecureOtpVerificationService;
public sealed class ConsoleEmailSender : IEmailSender
{
    public Task<bool> SendEmailAsync(string emailAddress, string emailBody,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Console.WriteLine($"\n[Email sent to {emailAddress}]");
        Console.WriteLine(emailBody);
        Console.WriteLine();
        return Task.FromResult(true);
    }
}
