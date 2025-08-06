using Elevators.Core.Constants;
using Elevators.Core.Interfaces;
using Elevators.Core.Models;
using Elevators.Core.Models.Enums;
using Elevators.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;
using Moq;
using Xunit;

namespace Elevators.Tests.Core
{
    public class ElevatorManagerServiceTests
    {
        private readonly Mock<IFeatureManager> _featureManagerMock;
        private readonly Mock<IHardwareIntegrationService> _hardwareServiceMock;
        private readonly Mock<Serilog.ILogger> _loggerMock;
        private readonly ElevatorManagerService _service;
        private readonly IConfiguration _configuration;
        private readonly IOptions<ElevatorSettings> _elevatorSettings;
        public ElevatorManagerServiceTests()
        {
            _featureManagerMock = new Mock<IFeatureManager>();
            _hardwareServiceMock = new Mock<IHardwareIntegrationService>();
            _loggerMock = new Mock<Serilog.ILogger>();

            var inMemorySettings = new Dictionary<string, string> {
            {"ElevatorOperatingRules:ServiceElevatorOperatingHours:Start", "08:00"},
            {"ElevatorOperatingRules:ServiceElevatorOperatingHours:End", "18:00"},
            {"ElevatorOperatingRules:UpDirectionOperatingHours:Start", "08:00"},
            {"ElevatorOperatingRules:UpDirectionOperatingHours:End", "12:00"},

        };
            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            _service = new ElevatorManagerService(
                _featureManagerMock.Object,
                _hardwareServiceMock.Object,
                _configuration,
                _loggerMock.Object
            );

            // Common mock setup for hardware
            _hardwareServiceMock.Setup(h => h.OpenDoorsAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(true);
            _hardwareServiceMock.Setup(h => h.CloseDoorsAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(true);
            _hardwareServiceMock.Setup(h => h.MoveElevatorAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Direction>())).ReturnsAsync(true);
        }

        [Fact]
        public async Task AddGeneralPassengerRequest_AssignsRequestToClosestElevator()
        {
            // Arrange
            _featureManagerMock.Setup(f => f.IsEnabledAsync(FeatureNames.PublicElevators)).ReturnsAsync(true);
            var publicElevator = _service.Elevators.First(e => e.Type == ElevatorType.Public);
            publicElevator.CurrentFloor = 5;

            // Act
            await _service.AddGeneralPassengerRequest(8, 2);

            // Assert
            Assert.Contains(2, publicElevator.SummonRequests);
            Assert.Single(_service.Floors[8].Passengers);
            Assert.True(_service.Floors[8].DownCall);
        }

        [Fact]
        public async Task AddGeneralPassengerRequest_ElevatorAlreadyAtFloor_AddsToDestinationRequests()
        {
            // Arrange
            _featureManagerMock.Setup(f => f.IsEnabledAsync(FeatureNames.PublicElevators)).ReturnsAsync(true);
            var publicElevator = _service.Elevators.First(e => e.Type == ElevatorType.Public);
            publicElevator.CurrentFloor = 3;

            // Act
            await _service.AddGeneralPassengerRequest(3, 7);

            // Assert
            Assert.Contains(7, publicElevator.SummonRequests);
            Assert.True(_service.Floors[3].UpCall);
        }

        [Fact]
        public async Task ProcessElevatorLogic_MovingElevator_ReachesDestinationAndEmptiesPassengers()
        {
            // Arrange
            _featureManagerMock.Setup(f => f.IsEnabledAsync(FeatureNames.PublicElevators)).ReturnsAsync(true);
            var publicElevator = _service.Elevators.First(e => e.Type == ElevatorType.Public);
            var passenger = new Passenger(1, 3, 5);
            publicElevator.CurrentFloor = 3;
            publicElevator.State = ElevatorState.MovingUp;
            publicElevator.AddPassenger(passenger);
            publicElevator.SummonRequests.Add(5);

            // Act
            publicElevator.CurrentFloor = 5; 
            await _service.ProcessElevatorCommands();

            // Assert
            Assert.Empty(publicElevator.Passengers);
            _hardwareServiceMock.Verify(h => h.OpenDoorsAsync(publicElevator.Id, 5), Times.Once);
            _hardwareServiceMock.Verify(h => h.CloseDoorsAsync(publicElevator.Id, 5), Times.Once);
        }

        [Fact]
        public async Task SetFireAlarm_ElevatorsEnterEmergencyStateAndGoToFloor1()
        {
            // Arrange
            var publicElevator = _service.Elevators.First(e => e.Type == ElevatorType.Public);
            publicElevator.CurrentFloor = 7;

            // Act
            _service.SetFireAlarm(true);
            await _service.ProcessElevatorCommands();

            // Assert
            Assert.True(_service.FireAlarmActive);
            Assert.Equal(ElevatorState.EmergencyStop, publicElevator.State);
            Assert.Contains(1, publicElevator.SummonRequests);
        }
    }
}
