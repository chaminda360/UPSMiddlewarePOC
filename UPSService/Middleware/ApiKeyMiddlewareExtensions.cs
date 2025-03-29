using Microsoft.AspNetCore.Builder;

public static class ApiKeyMiddlewareExtensions {
    public static IApplicationBuilder UseApiKeyMiddleware(this IApplicationBuilder builder) {
        return builder.UseMiddleware<ApiKeyMiddleware>();
    }
}