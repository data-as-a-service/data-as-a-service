using Daas.Api.Services;
using Daas.Api.Generation;
using Serilog;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "Daas.Api");

    var seqServerUrl = context.Configuration["Seq:ServerUrl"];
    if (!string.IsNullOrWhiteSpace(seqServerUrl))
    {
        configuration.WriteTo.Seq(seqServerUrl, apiKey: context.Configuration["Seq:ApiKey"]);
    }
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options => {
    options.AddPolicy("AllowCloudflareFrontend", policy => {
        policy.WithOrigins(
                        "https://data-as-a-service-frontend.saikrishnaias2004.workers.dev"
                      )
                      .AllowAnyHeader()
                      .AllowAnyMethod();
    });
});
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
builder.Services.AddSingleton<JsonFileStorageService>();
builder.Services.AddSingleton<SchemaService>();
builder.Services.AddScoped<ApiLinkService>();
builder.Services.AddSingleton<DatasetService>();


var app = builder.Build();

app.UseSerilogRequestLogging(options =>
{
    // Public API links contain bearer keys in the URL. Do not write request paths to logs.
    options.MessageTemplate = "HTTP {RequestMethod} request responded {StatusCode} in {Elapsed:0.0000} ms";
});

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("AllowCloudflareFrontend");
app.UseRateLimiter();
app.UseStaticFiles();
app.MapControllers();

app.Run();
