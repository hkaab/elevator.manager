using Elevators.Api.Services;
using Elevators.Core.Interfaces;
using Elevators.Core.Models;
using Elevators.Core.Services;
using Elevators.Core.Services.Mocks;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;
using Microsoft.OpenApi.Models;
using Serilog;
using Swashbuckle.AspNetCore.SwaggerGen;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        path: "logs/log.txt",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
    )
    .CreateLogger();

// Fix: Use the correct extension method for Serilog
builder.Host.UseSerilog(Log.Logger);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Feature Management
builder.Services.AddFeatureManagement(builder.Configuration.GetSection("FeatureFlags"));

// Register the core business logic and services
builder.Services.AddSingleton<IHardwareIntegrationService, MockHardwareIntegrationService>();

// Configure the ElevatorSettings
builder.Services.Configure<ElevatorSettings>(
    builder.Configuration.GetSection("ElevatorSettings"));

// Register the ElevatorManagerService as a singleton.
builder.Services.AddSingleton<IElevatorManagerService>(provider =>
    new ElevatorManagerService(
        provider.GetRequiredService<IFeatureManager>(),
        provider.GetRequiredService<IHardwareIntegrationService>(),
        provider.GetRequiredService<IConfiguration>(),
        provider.GetRequiredService<Serilog.ILogger>(),
        provider.GetRequiredService<IOptions<ElevatorSettings>>()));

// Register the hosted background service to run the elevator logic loop.
builder.Services.AddHostedService<BackgroundElevatorService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger(c => { c.OpenApiVersion = Microsoft.OpenApi.OpenApiSpecVersion.OpenApi2_0; });
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// The exception handler middleware we discussed
app.UseExceptionHandler("/error");

app.UseAuthorization();

app.MapControllers();

app.Run();

