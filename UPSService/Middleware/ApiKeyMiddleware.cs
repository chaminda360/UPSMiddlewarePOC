using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

/// <summary>
/// Middleware to validate API Key in the request headers.
/// </summary>
public class ApiKeyMiddleware {
    private readonly RequestDelegate _next;

    /// <summary>
    /// The name of the header containing the API Key.
    /// </summary>
    private const string ApiKeyHeaderName = "X-API-Key";

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiKeyMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    public ApiKeyMiddleware(RequestDelegate next) {
        _next = next;
    }

    /// <summary>
    /// Middleware logic to validate the API Key in the request headers.
    /// </summary>
    /// <param name="context">The HTTP context of the current request.</param>
    /// <param name="config">The application configuration to retrieve the expected API Key.</param>
    /// <returns>A task that represents the completion of the middleware execution.</returns>
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