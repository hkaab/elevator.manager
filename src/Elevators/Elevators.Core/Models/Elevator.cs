using Elevators.Core.Interfaces;
using Elevators.Core.Models.Enums;
using Serilog;

namespace Elevators.Core.Models
{
    /// Represents an elevator in the system with its properties and behaviors. startFloor is an optional parameter that defaults to 0 (Ground Level).
    public class Elevator (int id, int capacity, ElevatorType type, bool hasMusic, bool hasSpeaker, ILogger logger, int startFloor = 0) : IElevator 
    {
        public int Id { get; private set; } = id;
        public int CurrentFloor { get; set; } = startFloor;
        public ElevatorState State { get; set; } = ElevatorState.Idle;
        public Direction CurrentDirection { get; set; } = Direction.None;
        public List<Passenger> Passengers { get; private set; } = [];
        public int Capacity { get; private set; } = capacity;
        public bool HasMechanicalIssue { get; private set; } = false;
        public List<int> SummonRequests { get; private set; } = [];
        public ElevatorType Type { get; private set; } = type;
        public bool HasMusic { get; private set; } = hasMusic;
        public bool HasSpeaker { get; private set; } = hasSpeaker;
        public bool IsMusicPlaying { get; set; } = false;
        public bool IsEmergencyCallActive { get; set; } = false;
        private readonly ILogger _logger = logger;

        public void SetIssue(bool hasIssue)
        {
            HasMechanicalIssue = hasIssue;
            if (hasIssue)
            {
                State = ElevatorState.OutOfService;
                _logger.Error("--- Elevator {ElevatorId} ({ElevatorType}) has a mechanical issue and is now out of service! ---", Id, Type);
            }
            else
            {
                State = ElevatorState.Idle;
                _logger.Information("--- Elevator {ElevatorId} ({ElevatorType}) issue resolved. It is now {State}. ---", Id, Type, State);
            }
        }

        public bool IsFull()
        {
            return Passengers.Count >= Capacity;
        }

        public void AddPassenger(Passenger passenger)
        {
            if (!IsFull())
            {
                Passengers.Add(passenger);
                passenger.IsInsideElevator = true;
                if (!SummonRequests.Contains(passenger.DestinationFloor))
                {
                    SummonRequests.Add(passenger.DestinationFloor);
                    SummonRequests.Sort();
                }
                _logger.Information("Elevator {ElevatorId} ({ElevatorType}): Passenger {PassengerId} entered. Current passengers: {PassengerCount}/{Capacity}.", Id, Type, passenger.Id, Passengers.Count, Capacity);
            }
            else
            {
                _logger.Information("Elevator {ElevatorId} ({ElevatorType}): Cannot add Passenger {PassengerId}, elevator is full.", Id, Type, passenger.Id);
            }
        }

        public void RemovePassenger(Passenger passenger)
        {
            Passengers.Remove(passenger);
            passenger.IsInsideElevator = false;
            _logger.Information("Elevator {ElevatorId} ({ElevatorType}): Passenger {PassengerId} exited. Current passengers: {PassengerCount}/{Capacity}.", Id, Type, passenger.Id, Passengers.Count, Capacity);

            if (!Passengers.Any(p => p.DestinationFloor == passenger.DestinationFloor))
            {
                SummonRequests.Remove(passenger.DestinationFloor);
            }
        }

        public int GetNextDestination(int maxFloors)
        {
            if (SummonRequests.Count != 0)
            {
                if (CurrentDirection == Direction.Up)
                {
                    var nextUp = SummonRequests.Where(f => f > CurrentFloor).OrderBy(f => f).FirstOrDefault();
                    if (nextUp != 0) return nextUp;
                    var nextDown = SummonRequests.Where(f => f < CurrentFloor).OrderByDescending(f => f).FirstOrDefault();
                    if (nextDown != 0) return nextDown;
                }
                else if (CurrentDirection == Direction.Down)
                {
                    var nextDown = SummonRequests.Where(f => f < CurrentFloor).OrderByDescending(f => f).FirstOrDefault();
                    if (nextDown != 0) return nextDown;
                    var nextUp = SummonRequests.Where(f => f > CurrentFloor).OrderBy(f => f).FirstOrDefault();
                    if (nextUp != 0) return nextUp;
                }
                return SummonRequests.OrderBy(f => Math.Abs(f - CurrentFloor)).First();
            }
            return CurrentFloor;
        }

        public bool ShouldStop(IFloor currentFloor)
        {
            if (Passengers.Any(p => p.DestinationFloor == CurrentFloor) ||
               (CurrentDirection == Direction.Up && currentFloor.UpCall) ||
               (CurrentDirection == Direction.Down && currentFloor.DownCall) ||
               (State == ElevatorState.Idle && (currentFloor.UpCall || currentFloor.DownCall)))
            {
                return true;
            }
            return false;
        }
    }
}

