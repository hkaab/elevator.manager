using Elevators.Core.Interfaces;
using Elevators.Core.Services;

namespace Elevators.Api.Services
{
    public class BackgroundElevatorService(IServiceScopeFactory serviceScopeFactory, ILogger<BackgroundElevatorService> logger) : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;
        private readonly ILogger<BackgroundElevatorService> _logger = logger;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Background Elevators Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceScopeFactory.CreateScope();
                    var elevatorManagerService = scope.ServiceProvider.GetRequiredService<IElevatorManagerService>();
                    await ((ElevatorManagerService)elevatorManagerService).ProcessElevatorCommands();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An unhandled exception occurred in the background elevators service.");
                }

                await Task.Delay(1000, stoppingToken);
            }

            _logger.LogInformation("Background Elevators Service is stopping.");
        }
    }
}
