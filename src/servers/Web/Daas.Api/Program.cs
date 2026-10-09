using Daas.Api.Services;
using Daas.Api.Generation;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("public-mock", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

builder.Services.AddSingleton<FieldGeneratorFactory>();
builder.Services.AddSingleton<Random>();
builder.Services.AddHttpClient<OllamaDataGenerator>((services, client) =>
{
    var seconds = services.GetRequiredService<IConfiguration>().GetValue<int?>("Ollama:TimeoutSeconds") ?? 15;
    client.Timeout = TimeSpan.FromSeconds(Math.Clamp(seconds, 1, 120));
});
builder.Services.AddScoped<SchemaService>();
builder.Services.AddScoped<ApiLinkService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseRouting();
app.UseRateLimiter();
app.UseStaticFiles();
app.MapControllers();

app.Run();
