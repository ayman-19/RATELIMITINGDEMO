using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace RATELIMITINGDEMO;

public sealed class RateLimitingMiddleware(
    IMemoryCache _cache,
    ILogger<RateLimitingMiddleware> _logger
) : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var clientIp = context.Connection.RemoteIpAddress?.ToString();
        var endpoint = context.Request.Path;

        var cacheKey = $"rate_limit_{clientIp}_{endpoint}";
        var requestCount = _cache.Get<int>(cacheKey);
        var maxRequests = 3;
        var timeWindow = TimeSpan.FromMinutes(1);

        if (requestCount >= maxRequests)
        {
            _logger.LogWarning($"Rate limit exceeded for IP: {clientIp}, Endpoint: {endpoint}");

            context.Response.StatusCode = 429;
            context.Response.Headers["Retry-After"] = timeWindow.TotalSeconds.ToString();
            var resposne = new
            {
                status = 429,
                message = "Rate limit exceeded. Please try again later.",
                success = false,
            };

            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(resposne));
            return;
        }

        _cache.Set(
            cacheKey,
            requestCount + 1,
            new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = timeWindow }
        );

        context.Response.Headers["X-RateLimit-Limit"] = maxRequests.ToString();
        context.Response.Headers["X-RateLimit-Remaining"] = (
            maxRequests - requestCount - 1
        ).ToString();
        context.Response.Headers["X-RateLimit-Reset"] = DateTime
            .UtcNow.Add(timeWindow)
            .ToString("r");

        await next(context);
    }
}
