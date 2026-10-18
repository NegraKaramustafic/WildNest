using System.Text;
using eCommerce.Services;

namespace eCommerce.WebAPI.Services;

/// <summary>
/// Reads the account or email from the JSON body before the rate limiter runs,
/// then rewinds the body so model binding still sees it.
/// </summary>
public sealed class AuthRateLimitPreparationMiddleware
{
    private readonly RequestDelegate _next;

    public AuthRateLimitPreparationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (AuthRateLimitAccount.TryMatch(context.Request.Path.Value, context.Request.Method, out var kind))
        {
            context.Items[AuthRateLimitAccount.KindItemKey] = kind;
            context.Items[AuthRateLimitAccount.AccountItemKey] = AuthRateLimitAccount.FromBody(kind, await ReadSampleAsync(context));
        }

        await _next(context);
    }

    private static async Task<string> ReadSampleAsync(HttpContext context)
    {
        context.Request.EnableBuffering();
        if (context.Request.ContentLength is > 8192)
        {
            return string.Empty;
        }

        context.Request.Body.Position = 0;
        using var reader = new StreamReader(
            context.Request.Body,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            bufferSize: 1024,
            leaveOpen: true);
        var buffer = new char[8192];
        var count = await reader.ReadBlockAsync(buffer.AsMemory(0, buffer.Length), context.RequestAborted);
        context.Request.Body.Position = 0;
        return new string(buffer, 0, count);
    }
}
