using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace ELearningPlatform.API.Middleware;

/// <summary>
/// Resolves the file-logging directory from configuration ("FileLogging:LogDirectory").
/// The path may be absolute (e.g. "D:\\logs" or "/var/log/elearning") or relative to the
/// application content root (e.g. "logs"). The directory is created if it does not exist.
/// </summary>
public static class FileLogPath
{
    public static string Resolve(IConfiguration configuration, IWebHostEnvironment env)
    {
        var configured = configuration["FileLogging:LogDirectory"];
        if (string.IsNullOrWhiteSpace(configured))
        {
            configured = "logs";
        }

        var directory = Path.IsPathRooted(configured)
            ? configured
            : Path.Combine(env.ContentRootPath, configured);

        Directory.CreateDirectory(directory);
        return directory;
    }
}

/// <summary>
/// Writes one line per HTTP request (every action) to a daily rolling file.
/// Request/response bodies are intentionally NOT logged to avoid persisting
/// sensitive data such as passwords.
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _logDirectory;
    private readonly bool _enabled;
    private static readonly SemaphoreSlim _writeLock = new(1, 1);

    public RequestLoggingMiddleware(RequestDelegate next, IConfiguration configuration, IWebHostEnvironment env)
    {
        _next = next;
        _logDirectory = FileLogPath.Resolve(configuration, env);
        _enabled = configuration.GetValue("FileLogging:LogRequests", true);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_enabled)
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            await LogRequestAsync(context, stopwatch.ElapsedMilliseconds);
        }
    }

    private async Task LogRequestAsync(HttpContext context, long elapsedMs)
    {
        var request = context.Request;
        var user = context.User?.Identity?.IsAuthenticated == true
            ? context.User.FindFirst("userId")?.Value ?? context.User.Identity!.Name ?? "authenticated"
            : "anonymous";

        var line = string.Join(" | ", new[]
        {
            $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC]",
            $"{request.Method} {request.Path}{request.QueryString}",
            $"status={context.Response.StatusCode}",
            $"{elapsedMs}ms",
            $"user={user}",
            $"ip={context.Connection.RemoteIpAddress}"
        }) + Environment.NewLine;

        var filePath = Path.Combine(_logDirectory, $"requests-{DateTime.UtcNow:yyyy-MM-dd}.log");

        await _writeLock.WaitAsync();
        try
        {
            await File.AppendAllTextAsync(filePath, line, Encoding.UTF8);
        }
        finally
        {
            _writeLock.Release();
        }
    }
}

public static class RequestLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestFileLogging(this IApplicationBuilder app)
        => app.UseMiddleware<RequestLoggingMiddleware>();
}
