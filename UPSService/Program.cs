using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

// This is the entry point of the application.
// It configures services, middleware, and the HTTP request pipeline.

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Adds support for controllers in the application.
builder.Services.AddControllers();
// Configures Swagger/OpenAPI for API documentation and testing.
// Learn more at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register UPSApiService for dependency injection.
// Adds support for making HTTP requests.
builder.Services.AddHttpClient();
// Adds in-memory caching capabilities.
builder.Services.AddMemoryCache();
// Registers the UPSApiService with a scoped lifetime for dependency injection.
builder.Services.AddScoped<IUPSApiService, UPSApiService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Enables Swagger UI and API documentation in development mode.
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Enforces HTTPS for all incoming requests.
app.UseHttpsRedirection();

// Adds middleware to handle API key authentication.
app.UseApiKeyMiddleware();

// Adds middleware to handle authorization.
app.UseAuthorization();

// Maps controller endpoints to the request pipeline.
app.MapControllers();

// Starts the application.
app.Run();
