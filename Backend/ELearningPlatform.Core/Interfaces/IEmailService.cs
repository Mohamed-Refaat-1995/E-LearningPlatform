namespace ELearningPlatform.Core.Interfaces;

public interface IEmailService
{
    Task SendOtpEmailAsync(string toEmail, string otp);
    Task SendPasswordResetEmailAsync(string toEmail, string resetUrl);
}
