namespace SecureOtpVerificationService;
public interface IEmailSender
{
    Task<bool> SendEmailAsync(string emailAddress, string emailBody,
        CancellationToken cancellationToken = default);
}
