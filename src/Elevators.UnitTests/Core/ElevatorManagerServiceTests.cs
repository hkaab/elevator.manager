using Elevators.Core.Constants;
using Elevators.Core.Interfaces;
using Elevators.Core.Models;
using Elevators.Core.Models.Enums;
using Elevators.Core.Services;
using Microsoft.Extensions.Configuration;
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
        public async Task PassengerSummonGeneralElevatorRequest_OnceInGoUpFromGroundToLevel5()
        {
            // Arrange
            _featureManagerMock.Setup(f => f.IsEnabledAsync(FeatureNames.PublicElevators)).ReturnsAsync(true);
            var publicElevator = _service.Elevators.First(e => e.Value.Type == ElevatorType.Public).Value;
            publicElevator.CurrentFloor = 0;
            publicElevator.State = ElevatorState.Idle;
            ElevatorCommandRequest request = new()
            {
                ElevatorCommand = ElevatorCommand.SummonGeneralElevator,
                FromFloor = 0,
                ToFloor = 5
            };

            // Act
            await _service.QueueElevatorCommandRequest(request);
            await _service.ProcessElevatorCommands();

            // Assert
            Assert.Equal(Direction.Up, publicElevator.CurrentDirection);
            Assert.Equal(5, publicElevator.CurrentFloor);
            Assert.Equal(ElevatorState.MovingUp, publicElevator.State);
        }

        [Fact]
        public async Task PassengerSummonGeneralElevatorRequest_ToGoDownFromLevel6and4BothGoToLevel1()
        {
            // Arrange
            _featureManagerMock.Setup(f => f.IsEnabledAsync(FeatureNames.PublicElevators)).ReturnsAsync(true);
            var publicElevator = _service.Elevators.First(e => e.Value.Type == ElevatorType.Public).Value;
            publicElevator.CurrentFloor = 0;
            publicElevator.State = ElevatorState.Idle;
            ElevatorCommandRequest request1 = new()
            {
                ElevatorCommand = ElevatorCommand.SummonGeneralElevator,
                FromFloor = 6,
                ToFloor = 1
            };

            ElevatorCommandRequest request2 = new()
            {
                ElevatorCommand = ElevatorCommand.SummonGeneralElevator,
                FromFloor = 4,
                ToFloor = 1
            };
            // Act
            await _service.QueueElevatorCommandRequest(request1);
            await _service.QueueElevatorCommandRequest(request2);
            // up
            await _service.ProcessElevatorCommands();
            // up again
            await _service.ProcessElevatorCommands();
            // down
            await _service.ProcessElevatorCommands();

            // Assert
            Assert.Equal(Direction.Down, publicElevator.CurrentDirection);
            Assert.Equal(1, publicElevator.CurrentFloor);
            Assert.Equal(ElevatorState.MovingDown, publicElevator.State);
        }
        [Fact]
        public async Task PassengerSummonGeneralElevatorRequest_L2ToL6_L4ToG()
        {
            // Arrange
            _featureManagerMock.Setup(f => f.IsEnabledAsync(FeatureNames.PublicElevators)).ReturnsAsync(true);
            var publicElevator = _service.Elevators.First(e => e.Value.Type == ElevatorType.Public).Value;
            publicElevator.CurrentFloor = 0;
            publicElevator.State = ElevatorState.Idle;

            ElevatorCommandRequest request1 = new()
            {
                ElevatorCommand = ElevatorCommand.SummonGeneralElevator,
                FromFloor = 2,
                ToFloor = 6
            };

            ElevatorCommandRequest request2 = new()
            {
                ElevatorCommand = ElevatorCommand.SummonGeneralElevator,
                FromFloor = 4,
                ToFloor = 0
            };

            // Act
            await _service.QueueElevatorCommandRequest(request1);
            await _service.QueueElevatorCommandRequest(request2);
            await _service.ProcessElevatorCommands();
            await _service.ProcessElevatorCommands();
            await _service.ProcessElevatorCommands();
            await _service.ProcessElevatorCommands();

            // Assert
            Assert.Equal(Direction.Down, publicElevator.CurrentDirection);
            Assert.Equal(0, publicElevator.CurrentFloor);
            Assert.Equal(ElevatorState.MovingDown, publicElevator.State);
        }

        [Fact]
        public async Task PassengerSummonGeneralElevatorRequest_L0ToL5_L4ToG_L10ToG()
        {
            // Arrange
            _featureManagerMock.Setup(f => f.IsEnabledAsync(FeatureNames.PublicElevators)).ReturnsAsync(true);
            var publicElevator = _service.Elevators.First(e => e.Value.Type == ElevatorType.Public).Value;
            publicElevator.CurrentFloor = 0;
            publicElevator.State = ElevatorState.Idle;

            ElevatorCommandRequest request1 = new()
            {
                ElevatorCommand = ElevatorCommand.SummonGeneralElevator,
                FromFloor = 0,
                ToFloor = 5
            };

            ElevatorCommandRequest request2 = new()
            {
                ElevatorCommand = ElevatorCommand.SummonGeneralElevator,
                FromFloor = 4,
                ToFloor = 0
            };

            ElevatorCommandRequest request3 = new()
            {
                ElevatorCommand = ElevatorCommand.SummonGeneralElevator,
                FromFloor = 10,
                ToFloor = 0
            };

            // Act
            await _service.QueueElevatorCommandRequest(request1);
            await _service.QueueElevatorCommandRequest(request2);
            await _service.QueueElevatorCommandRequest(request3);

            await _service.ProcessElevatorCommands();
            await _service.ProcessElevatorCommands();
            await _service.ProcessElevatorCommands();
            await _service.ProcessElevatorCommands();

            // Assert
            Assert.Equal(Direction.Down, publicElevator.CurrentDirection);
            Assert.Equal(0, publicElevator.CurrentFloor);
            Assert.Equal(ElevatorState.MovingDown, publicElevator.State);
        }

        [Fact]
        public async Task AddSummonGeneralElevatorRequest_AssignsRequestToClosestElevator()
        {
            // Arrange
            _featureManagerMock.Setup(f => f.IsEnabledAsync(FeatureNames.PublicElevators)).ReturnsAsync(true);
            var publicElevator = _service.Elevators.First(e => e.Value.Type == ElevatorType.Public).Value;
            publicElevator.CurrentFloor = 5;
            ElevatorCommandRequest request = new()
            {
                ElevatorCommand = ElevatorCommand.SummonGeneralElevator,
                FromFloor = 8,
                ToFloor = 2
            };

            // Act
            await _service.QueueElevatorCommandRequest(request);
            await _service.ProcessElevatorCommands();

            // Assert
            Assert.Contains(8, publicElevator.SummonRequests);
            Assert.Single(_service.Floors[8].Passengers);
            Assert.True(_service.Floors[8].DownCall);
        }

        [Fact]
        public async Task AddSummonGeneralElevatorRequest_ElevatorAlreadyAtFloor_GoToDestinationRequests()
        {
            // Arrange
            _featureManagerMock.Setup(f => f.IsEnabledAsync(FeatureNames.PublicElevators)).ReturnsAsync(true);
            var publicElevator = _service.Elevators.First(e => e.Value.Type == ElevatorType.Public).Value;
            publicElevator.CurrentFloor = 3;

            ElevatorCommandRequest request = new()
            {
                ElevatorCommand = ElevatorCommand.SummonGeneralElevator,
                FromFloor = 3,
                ToFloor = 7
            };

            // Act
            await _service.QueueElevatorCommandRequest(request);
            await _service.ProcessElevatorCommands();

            // Assert
            Assert.Equal(7, publicElevator.CurrentFloor);
            Assert.Single(publicElevator.Passengers);
        }

        [Fact]
        public async Task ProcessElevatorLogic_MovingElevator_ReachesDestinationAndEmptiesPassengers()
        {
            // Arrange
            _featureManagerMock.Setup(f => f.IsEnabledAsync(FeatureNames.PublicElevators)).ReturnsAsync(true);
            // Arrange an elevator that is moving and has passengers
            var publicElevator = _service.Elevators.First(e => e.Value.Type == ElevatorType.Public).Value;
            var passenger = new Passenger(1, 3, 5);
            publicElevator.State = ElevatorState.MovingUp;
            publicElevator.AddPassenger(passenger);
            publicElevator.SummonRequests.Add(5);
            publicElevator.CurrentFloor = 5;

            // Act
            await _service.ProcessElevatorCommands();

            // Assert
            Assert.Empty(publicElevator.Passengers);
            _hardwareServiceMock.Verify(h => h.OpenDoorsAsync(publicElevator.Id, 5), Times.Once);
            _hardwareServiceMock.Verify(h => h.CloseDoorsAsync(publicElevator.Id, 5), Times.Once);
        }

        [Fact]
        public async Task SetFireAlarm_ElevatorsEnterEmergencyStateAndGoToGroundFloor()
        {
            // Arrange
            var publicElevator = _service.Elevators.First(e => e.Value.Type == ElevatorType.Public).Value;
            publicElevator.CurrentFloor = 7;

            ElevatorCommandRequest request = new()
            {
                ElevatorCommand = ElevatorCommand.FireAlarm,
                FireAlarmActive = true,
            };

            // Act
            await _service.QueueElevatorCommandRequest(request);
            await _service.ProcessElevatorCommands();

            // Assert
            Assert.True(_service.FireAlarmActive);
            Assert.Equal(ElevatorState.EmergencyStop, publicElevator.State);
            Assert.Contains(0, publicElevator.SummonRequests);
        }

    }
}
