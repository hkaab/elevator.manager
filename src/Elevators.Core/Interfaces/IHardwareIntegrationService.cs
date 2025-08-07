using Elevators.Core.Models.Enums;

namespace Elevators.Core.Interfaces
{
    public interface IHardwareIntegrationService
    {
        Task<bool> OpenDoorsAsync(int elevatorId, int floorNumber);
        Task<bool> CloseDoorsAsync(int elevatorId, int floorNumber);
        Task<bool> MoveElevatorAsync(int elevatorId, int FromFloor, int targetFloor, Direction direction);
        Task<bool> SetElevatorIssueAsync(int elevatorId, bool hasIssue);
        Task<bool> ActivateFireAlarmAsync();
        Task<bool> DeactivateFireAlarmAsync();
        Task<bool> PlayMusicAsync(int elevatorId);
        Task<bool> StopMusicAsync(int elevatorId);
        Task<bool> ActivateEmergencyCallAsync(int elevatorId);
        Task<bool> SpeakFloorNumberAsync(int elevatorId, int floorNumber);
    }
}
