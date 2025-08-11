using Elevators.Core.Interfaces;
using Elevators.Core.Models.Enums;
using Serilog;

namespace Elevators.Core.Services.Mocks
{
           
    public class MockHardwareIntegrationService : IHardwareIntegrationService
    {
        private readonly Random _random = new();
        private const double FailureChance = 0.1;
        private readonly ILogger _logger;

        public MockHardwareIntegrationService(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<bool> OpenDoorsAsync(int elevatorId, int floorNumber)
        {
            _logger.Information("[HW MOCK]: Elevator {ElevatorId} at Floor {FloorNumber}: Attempting to Open Doors...", elevatorId, floorNumber);
            await Task.Delay(500);
            if (_random.NextDouble() < FailureChance)
            {
                _logger.Error("[HW MOCK]: Elevator {ElevatorId} at Floor {FloorNumber}: Door Open FAILED!", elevatorId, floorNumber);
                return false;
            }
            _logger.Information("[HW MOCK]: Elevator {ElevatorId} at Floor {FloorNumber}: Doors Opened.", elevatorId, floorNumber);
            return true;
        }

        public async Task<bool> CloseDoorsAsync(int elevatorId, int floorNumber)
        {
            _logger.Information("[HW MOCK]: Elevator {ElevatorId} at Floor {FloorNumber}: Attempting to Close Doors...", elevatorId, floorNumber);
            await Task.Delay(500);
            if (_random.NextDouble() < FailureChance)
            {
                _logger.Error("[HW MOCK]: Elevator {ElevatorId} at Floor {FloorNumber}: Door Close FAILED!", elevatorId, floorNumber);
                return false;
            }
            _logger.Information("[HW MOCK]: Elevator {ElevatorId} at Floor {FloorNumber}: Doors Closed.", elevatorId, floorNumber);
            return true;
        }

        public async Task<bool> MoveElevatorAsync(int elevatorId, int FromFloor, int targetFloor, Direction direction)
        {
            _logger.Information("[HW MOCK]: Elevator {ElevatorId}: Attempting to Move from {FromFloor} to {TargetFloor} ({Direction})...", elevatorId, FromFloor, targetFloor, direction);
            await Task.Delay(1000);
            if (_random.NextDouble() < FailureChance)
            {
                _logger.Error("[HW MOCK]: Elevator {ElevatorId}: Movement FAILED between {FromFloor} and {TargetFloor}!", elevatorId, FromFloor, targetFloor);
                return false;
            }
            _logger.Information("[HW MOCK]: Elevator {ElevatorId}: Successfully moved to {TargetFloor}.", elevatorId, targetFloor);
            return true;
        }

        public async Task<bool> SetElevatorIssueAsync(int elevatorId, bool hasIssue)
        {
            _logger.Information("[HW MOCK]: Elevator {ElevatorId}: Attempting to {Action} issue state...", elevatorId, hasIssue ? "SET" : "CLEAR");
            await Task.Delay(200);
            if (_random.NextDouble() < FailureChance)
            {
                _logger.Error("[HW MOCK]: Elevator {ElevatorId}: Set Issue FAILED!", elevatorId);
                return false;
            }
            _logger.Information("[HW MOCK]: Elevator {ElevatorId}: Issue state {Action}.", elevatorId, hasIssue ? "SET" : "CLEARED");
            return true;
        }

        public async Task<bool> ActivateFireAlarmAsync()
        {
            _logger.Warning("[HW MOCK]: Attempting to ACTIVATE Fire Alarm...");
            await Task.Delay(500);
            if (_random.NextDouble() < FailureChance)
            {
                _logger.Error("[HW MOCK]: Fire Alarm Activation FAILED!");
                return false;
            }
            _logger.Error("[HW MOCK]: Fire Alarm ACTIVATED.");
            return true;
        }

        public async Task<bool> DeactivateFireAlarmAsync()
        {
            _logger.Warning("[HW MOCK]: Attempting to DEACTIVATE Fire Alarm...");
            await Task.Delay(500);
            if (_random.NextDouble() < FailureChance)
            {
                _logger.Error("[HW MOCK]: Fire Alarm Deactivation FAILED!");
                return false;
            }
            _logger.Information("[HW MOCK]: Fire Alarm DEACTIVATED.");
            return true;
        }

        public async Task<bool> PlayMusicAsync(int elevatorId)
        {
            _logger.Information("[HW MOCK]: Elevator {ElevatorId}: Attempting to Play Music...", elevatorId);
            await Task.Delay(100);
            if (_random.NextDouble() < FailureChance)
            {
                _logger.Error("[HW MOCK]: Elevator {ElevatorId}: Play Music FAILED!", elevatorId);
                return false;
            }
            _logger.Information("[HW MOCK]: Elevator {ElevatorId}: Music is now playing.", elevatorId);
            return true;
        }

        public async Task<bool> StopMusicAsync(int elevatorId)
        {
            _logger.Information("[HW MOCK]: Elevator {ElevatorId}: Attempting to Stop Music...", elevatorId);
            await Task.Delay(100);
            if (_random.NextDouble() < FailureChance)
            {
                _logger.Error("[HW MOCK]: Elevator {ElevatorId}: Stop Music FAILED!", elevatorId);
                return false;
            }
            _logger.Information("[HW MOCK]: Elevator {ElevatorId}: Music has stopped.", elevatorId);
            return true;
        }

        public async Task<bool> ActivateEmergencyCallAsync(int elevatorId)
        {
            _logger.Error("[HW MOCK]: Elevator {ElevatorId}: Activating emergency call system...", elevatorId);
            await Task.Delay(200);
            _logger.Error("[HW MOCK]: Elevator {ElevatorId}: Emergency call signal sent to control center.", elevatorId);
            return true;
        }

        public async Task<bool> SpeakFloorNumberAsync(int elevatorId, int floorNumber)
        {
            _logger.Information("[HW MOCK]: Elevator {ElevatorId}: Voice announcement: 'We are in floor {FloorNumber}.'", elevatorId, floorNumber);
            await Task.Delay(300);
            return true;
        }
    }
}
