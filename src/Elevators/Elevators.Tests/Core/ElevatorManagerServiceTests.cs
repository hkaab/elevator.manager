using Moq;
using Microsoft.Extensions.Configuration;
using Microsoft.FeatureManagement;
namespace Elevators.Tests.Core
{
    public class ElevatorManagerServiceTests
    {
        private readonly Mock<IFeatureManager> _featureManagerMock;
        private readonly Mock<IHardwareIntegrationService> _hardwareServiceMock;
        private readonly Mock<ILogger<ElevatorManagerService>> _loggerMock;
        private readonly ElevatorManagerService _service;
        private readonly IConfiguration _configuration;
        public ElevatorManagerServiceTests()
        {
            _featureManagerMock = new Mock<IFeatureManager>();
            _hardwareServiceMock = new Mock<IHardwareIntegrationService>();
            _loggerMock = new Mock<ILogger<ElevatorManagerService>>();

            var inMemorySettings = new Dictionary<string, string> {
            {"ElevatorOperatingRules:ServiceElevatorOperatingHours:Start", "08:00"},
            {"ElevatorOperatingRules:ServiceElevatorOperatingHours:End", "18:00"},
            {"ElevatorOperatingRules:UpDirectionOperatingHours:Start", "08:00"},
            {"ElevatorOperatingRules:UpDirectionOperatingHours:End", "12:00"}
        };
            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            _service = new ElevatorManagerService(
                _featureManagerMock.Object,
                _hardwareServiceMock.Object,
                _configuration,
                _loggerMock.Object,
                numberOfPublicElevators: 1,
                numberOfPrivateElevators: 1,
                numberOfServiceElevators: 0, // Simplified for this test
                maxFloors: 10,
                elevatorCapacity: 5,
                publicElevatorHasMusic: false,
                publicElevatorHasSpeaker: false,
                privateElevatorHasMusic: false,
                privateElevatorHasSpeaker: false,
                serviceElevatorHasMusic: false,
                serviceElevatorHasSpeaker: false
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
            publicElevators.CurrentFloor = 5;

            // Act
            await _service.AddGeneralPassengerRequest(8, 2);

            // Assert
            Assert.Contains(8, publicElevators.DestinationRequests);
            Assert.Equal(1, _service.Floors[7].WaitingPassengers.Count);
        }

        [Fact]
        public async Task AddGeneralPassengerRequest_ElevatorAlreadyAtFloor_AddsToDestinationRequests()
        {
            // Arrange
            _featureManagerMock.Setup(f => f.IsEnabledAsync(FeatureNames.PublicElevators)).ReturnsAsync(true);
            var publicElevator = _service.Elevators.First(e => e.Type == ElevatorType.Public);
            publicElevators.CurrentFloor = 3;

            // Act
            await _service.AddGeneralPassengerRequest(3, 7);

            // Assert
            Assert.Contains(3, publicElevators.DestinationRequests);
            Assert.Contains(7, publicElevators.DestinationRequests);
            Assert.True(_service.Floors[2].UpCall);
        }

        [Fact]
        public async Task ProcessElevatorLogic_MovingElevator_ReachesDestinationAndEmptiesPassengers()
        {
            // Arrange
            _featureManagerMock.Setup(f => f.IsEnabledAsync(FeatureNames.PublicElevators)).ReturnsAsync(true);
            var publicElevator = _service.Elevators.First(e => e.Type == ElevatorType.Public);
            var passenger = new Passenger(1, 3, 5);
            publicElevators.CurrentFloor = 3;
            publicElevators.State = ElevatorState.MovingUp;
            publicElevators.AddPassenger(passenger);
            publicElevators.DestinationRequests.Add(5);

            // Act
            publicElevators.CurrentFloor = 5; // Simulate movement to the destination
            await _service.ProcessElevatorLogic();

            // Assert
            Assert.Equal(0, publicElevators.Passengers.Count);
            _hardwareServiceMock.Verify(h => h.OpenDoorsAsync(publicElevators.Id, 5), Times.Once);
            _hardwareServiceMock.Verify(h => h.CloseDoorsAsync(publicElevators.Id, 5), Times.Once);
        }

        [Fact]
        public async Task SetFireAlarm_ElevatorsEnterEmergencyStateAndGoToFloor1()
        {
            // Arrange
            var publicElevator = _service.Elevators.First(e => e.Type == ElevatorType.Public);
            publicElevators.CurrentFloor = 7;

            // Act
            _service.SetFireAlarm(true);
            await _service.ProcessElevatorLogic();

            // Assert
            Assert.True(_service.FireAlarmActive);
            Assert.Equal(ElevatorState.EmergencyStop, publicElevators.State);
            Assert.Contains(1, publicElevators.DestinationRequests);
        }
    }
}
