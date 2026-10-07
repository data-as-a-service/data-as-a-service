using Daas.Api.Services;
using Daas.Api.Generation;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<FieldGeneratorFactory>();
builder.Services.AddSingleton<Random>();
builder.Services.AddScoped<SchemaService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.MapControllers();

app.Run();
