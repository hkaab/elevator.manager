using Elevators.Core.Models;
using System.Collections.Concurrent;

namespace Elevators.Core.Interfaces
{
    public interface IElevatorManagerService
    {
        ConcurrentQueue<ElevatorCommandRequest> ElevatorCommandsQueue { get; }
        List<IElevator> Elevators { get; }
        Dictionary<int,IFloor> Floors { get; }
        Task QueueElevatorCommandRequest(ElevatorCommandRequest elevatorCommandRequest);
        Task ProcessElevatorCommands();
        bool FireAlarmActive { get; }

    }
}
