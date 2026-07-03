using ELearningPlatform.Core.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;

namespace ELearningPlatform.Application.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;

    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendOtpEmailAsync(string toEmail, string otp)
    {
        var subject = "Verify Your eLearn Account";
        var body = $@"
            <div style='font-family:Arial,sans-serif;max-width:480px;margin:0 auto;padding:24px;background:#f9fafb;border-radius:12px;'>
              <h2 style='color:#4f46e5;margin-bottom:8px;'>Email Verification</h2>
              <p style='color:#374151;'>Welcome to eLearn! Use the verification code below to confirm your email address.</p>
              <div style='background:#fff;border:2px solid #e0e7ff;border-radius:8px;padding:24px;text-align:center;margin:24px 0;'>
                <p style='color:#6b7280;font-size:14px;margin:0 0 8px;'>Your OTP code</p>
                <span style='font-size:36px;font-weight:700;letter-spacing:8px;color:#4f46e5;'>{otp}</span>
              </div>
              <p style='color:#6b7280;font-size:13px;'>This code expires in 15 minutes. If you did not create an account, please ignore this email.</p>
            </div>";

        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string resetUrl)
    {
        var subject = "Reset Your eLearn Password";
        var body = $@"
        <div style='font-family:Arial,sans-serif;max-width:480px;margin:0 auto;padding:24px;background:#f9fafb;border-radius:12px;'>
          <h2 style='color:#4f46e5;margin-bottom:8px;'>Password Reset Request</h2>
          <p style='color:#374151;'>We received a request to reset your password. Click the button below to set a new password.</p>
          <div style='text-align:center;margin:28px 0;'>
            <a href='{resetUrl}' style='background:#4f46e5;color:#fff;padding:12px 28px;border-radius:8px;text-decoration:none;font-weight:600;font-size:15px;'>
              Reset Password
            </a>
          </div>
        </div>";

        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendCertificateReadyEmailAsync(string toEmail, string studentName, string courseTitle, string certificateUrl)
    {
        var subject = "🎉 Congratulations — Your Certificate is Ready!";
        var body = $@"
        <div style='font-family:Arial,sans-serif;max-width:480px;margin:0 auto;padding:24px;background:#f9fafb;border-radius:12px;'>
          <h2 style='color:#4f46e5;margin-bottom:8px;'>Congratulations, {studentName}! 🎓</h2>
          <p style='color:#374151;'>You've successfully completed <strong>{courseTitle}</strong> — every lesson watched and every quiz passed. Your certificate of completion is ready.</p>
          <div style='text-align:center;margin:28px 0;'>
            <a href='{certificateUrl}' style='background:#4f46e5;color:#fff;padding:12px 28px;border-radius:8px;text-decoration:none;font-weight:600;font-size:15px;'>
              View &amp; Download Certificate
            </a>
          </div>
          <p style='color:#6b7280;font-size:13px;'>Well done, and keep learning!</p>
        </div>";

        await SendEmailAsync(toEmail, subject, body);
    }

    private async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
        var settings = _configuration.GetSection("EmailSettings");
        var host = settings["Host"] ?? "smtp.gmail.com";
        var port = int.Parse(settings["Port"] ?? "587");
        var username = settings["Username"] ?? "";
        var password = settings["Password"] ?? "";
        var fromName = settings["FromName"] ?? "eLearn Platform";

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, username));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = htmlBody };

        using var client = new SmtpClient();
        await client.ConnectAsync(host, port, SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(username, password);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}
