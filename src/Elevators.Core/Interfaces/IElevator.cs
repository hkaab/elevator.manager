using Elevators.Core.Models;
using Elevators.Core.Models.Enums;

namespace Elevators.Core.Interfaces
{
    public interface IElevator
    {
        int Id { get; }
        int CurrentFloor { get; set; }
        ElevatorState State { get; set; }
        Direction CurrentDirection { get; set; }
        List<Passenger> Passengers { get; }
        int Capacity { get; }
        bool HasMechanicalIssue { get; }

        List<int> SummonRequests { get; }
        ElevatorType Type { get; }
        bool HasMusic { get; }
        bool HasSpeaker { get; }
        bool IsMusicPlaying { get; set; }
        bool IsEmergencyCallActive { get; set; }
        bool IsFull();
        void SetIssue(bool hasIssue);
        void AddPassenger(Passenger passenger);
        void RemovePassenger(Passenger passenger);
        int GetNextDestination();
        bool ShouldStop(IFloor FromFloorRequests);
    }
}
