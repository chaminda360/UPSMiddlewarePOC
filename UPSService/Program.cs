using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register UPSApiService for dependency injection
builder.Services.AddHttpClient();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IUPSApiService, UPSApiService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.UseApiKeyMiddleware();

app.MapControllers();

app.Run();
