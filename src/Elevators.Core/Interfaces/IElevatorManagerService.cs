using Elevators.Core.Models;

namespace Elevators.Core.Interfaces
{
    public interface IElevatorManagerService
    {
        List<IElevator> Elevators { get; }
        List<IFloor> Floors { get; }
        bool FireAlarmActive { get; }
        void SetFireAlarm(bool active);
        Task AddGeneralPassengerRequest(int currentFloor, int destinationFloor);
        Task AddPrivateElevatorRequest(int elevatorId, int currentFloor, int destinationFloor);
        Task AddServiceElevatorRequest(int currentFloor, int destinationFloor, bool hasSwappedCard);
        void SetElevatorIssue(int elevatorId, bool hasIssue);
        Task EmergencyCallAsync(int elevatorId);
        Task ProcessElevatorCommands();
    }
}
