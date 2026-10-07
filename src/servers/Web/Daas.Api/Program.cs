using Daas.Api.Services;
using Daas.Application.Users.Queries;
using Daas.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSingleton<FieldGeneratorFactory>();
builder.Services.AddSingleton<Random>();
builder.Services.AddScoped<SchemaService>();
builder.Services.AddScoped<UserService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.MapControllers();

app.Run();
