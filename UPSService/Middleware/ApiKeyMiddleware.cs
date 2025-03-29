using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

public class ApiKeyMiddleware {
    private readonly RequestDelegate _next;
    private const string ApiKeyHeaderName = "X-API-Key";

    public ApiKeyMiddleware(RequestDelegate next) {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IConfiguration config) {
        if (!context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var extractedApiKey)) {
            context.Response.StatusCode = 401; // Unauthorized
            await context.Response.WriteAsync("API Key is missing");
            return;
        }

        var configuredApiKey = config["ApiKey"];

        if (string.IsNullOrEmpty(configuredApiKey) || !configuredApiKey.Equals(extractedApiKey)) {
            context.Response.StatusCode = 403; // Forbidden
            await context.Response.WriteAsync("Invalid or missing API Key");
            return;
        }

        await _next(context);
    }
}