using SecureOtpVerificationService;

var emailSender = new ConsoleEmailSender();
using var otpService = new EmailOtpService(emailSender);

otpService.Start();

Console.Write("Enter DSO email address: ");
var email = Console.ReadLine() ?? string.Empty;

var emailStatus = await otpService.GenerateOtpEmailAsync(email);
Console.WriteLine($"Email status: {emailStatus}");

if (emailStatus == OtpStatus.STATUS_EMAIL_OK)
{
    var otpStatus = await otpService.CheckOtpAsync(new ConsoleOtpInput());
    Console.WriteLine($"OTP status: {otpStatus}");
}

otpService.Close();
