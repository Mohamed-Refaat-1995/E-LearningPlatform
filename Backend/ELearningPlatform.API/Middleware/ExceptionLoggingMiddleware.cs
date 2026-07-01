using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Threading;

namespace ELearningPlatform.API.Middleware;

public class ExceptionLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _logDirectory;
    private static readonly SemaphoreSlim _writeLock = new(1, 1);

    public ExceptionLoggingMiddleware(RequestDelegate next, IConfiguration configuration, IWebHostEnvironment env)
    {
        _next = next;
        _logDirectory = FileLogPath.Resolve(configuration, env);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await LogExceptionToFileAsync(ex, context);
            throw;
        }
    }

    private async Task LogExceptionToFileAsync(Exception ex, HttpContext context)
    {
        var fileName = $"exceptions-{DateTime.UtcNow:yyyy-MM-dd}.log";
        var filePath = Path.Combine(_logDirectory, fileName);

        var sb = new StringBuilder();
        sb.AppendLine("=".PadRight(80, '='));
        sb.AppendLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC]  UNHANDLED EXCEPTION");
        sb.AppendLine($"Path      : {context.Request.Method} {context.Request.Path}{context.Request.QueryString}");
        sb.AppendLine($"IP        : {context.Connection.RemoteIpAddress}");
        sb.AppendLine($"User      : {context.User?.Identity?.Name ?? "anonymous"}");
        sb.AppendLine($"Type      : {ex.GetType().FullName}");
        sb.AppendLine($"Message   : {ex.Message}");

        var inner = ex.InnerException;
        while (inner != null)
        {
            sb.AppendLine($"InnerType : {inner.GetType().FullName}");
            sb.AppendLine($"InnerMsg  : {inner.Message}");
            inner = inner.InnerException;
        }

        sb.AppendLine("StackTrace:");
        sb.AppendLine(ex.StackTrace);
        sb.AppendLine();

        await _writeLock.WaitAsync();
        try
        {
            await File.AppendAllTextAsync(filePath, sb.ToString(), Encoding.UTF8);
        }
        finally
        {
            _writeLock.Release();
        }
    }
}

public static class ExceptionLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionFileLogging(this IApplicationBuilder app)
        => app.UseMiddleware<ExceptionLoggingMiddleware>();
}
