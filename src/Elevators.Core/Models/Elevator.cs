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
                if (!SummonRequests.Contains(passenger.FromFloor))
                {
                    SummonRequests.Add(passenger.FromFloor);
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

            if (!Passengers.Any(p => p.ToFloor == passenger.ToFloor))
            {
                SummonRequests.Remove(passenger.FromFloor);
            }
        }

        public int GetNextDestination()
        {
            if (SummonRequests.Count != 0 || Passengers.Count != 0)
            {
                if (CurrentDirection == Direction.Up)
                {
                    var nextUp = SummonRequests.Where(f => f > CurrentFloor).OrderBy(f => f).FirstOrDefault();
                    var nextStop = Passengers.Where(p => p.ToFloor > CurrentFloor).Select(p => p.ToFloor).OrderBy(f => f).FirstOrDefault();

                    // going down
                    if (nextUp == 0 && nextStop == 0)
                    {
                        nextStop = Passengers.Where(p => p.ToFloor < CurrentFloor).Select(p => p.ToFloor).OrderBy(f => f).FirstOrDefault();
                        return nextStop;
                    }

                    if ((nextUp >= 0 && nextUp < nextStop && nextUp> CurrentFloor) || nextUp > CurrentFloor)
                        return nextUp;

                    if (nextStop > CurrentFloor)
                        return nextStop;

                    var nextDown = SummonRequests.Where(f => f < CurrentFloor).OrderByDescending(f => f).FirstOrDefault();
                    if ((nextDown != 0 && nextStop < nextDown) || nextDown < CurrentFloor)
                        return nextDown;

                    if (nextStop >= 0)
                        return nextStop;
                }
                else if (CurrentDirection == Direction.Down)
                {
                    var nextDown = SummonRequests.Where(f => f < CurrentFloor).OrderByDescending(f => f).FirstOrDefault();
                    var nextStop = Passengers.Where(p => p.ToFloor < CurrentFloor).Select(p => p.ToFloor).OrderByDescending(f => f).FirstOrDefault();
                    if (nextDown >= 0 && nextDown > nextStop)
                        return nextDown;
                    var nextUp = SummonRequests.Where(f => f > CurrentFloor).OrderBy(f => f).FirstOrDefault();
                    if (nextUp >= 0 && nextStop > nextUp)
                        return nextUp;
                    if (nextStop >= 0)
                        return nextStop;
                }
                var nextSummonedStop = -1;
                if (SummonRequests.Count > 0)
                    nextSummonedStop = SummonRequests.OrderBy(f => Math.Abs(f - CurrentFloor)).First();
                var nextPassengerStop = Passengers.OrderBy(p => Math.Abs(p.ToFloor - CurrentFloor)).Select(p => p.ToFloor).FirstOrDefault();
                if (nextSummonedStop >= 0 && nextPassengerStop >= 0)
                {
                    if (nextSummonedStop == CurrentFloor)
                        return nextPassengerStop;
                    else if (nextPassengerStop == CurrentFloor)
                        return nextSummonedStop;
                    if (Math.Abs(nextSummonedStop - CurrentFloor) < Math.Abs(nextPassengerStop - CurrentFloor))
                        return nextSummonedStop;
                    else
                        return nextPassengerStop;
                }
                else if (nextSummonedStop >= 0)
                {
                    return nextSummonedStop;
                }
                else if (nextPassengerStop >= 0)
                {
                    return nextPassengerStop;
                }
            }
            return CurrentFloor;
        }

        public bool ShouldStop(IFloor FromFloor)
        {
            if (Passengers.Any(p => p.ToFloor == FromFloor.FloorNumber) ||
               (CurrentDirection == Direction.Up && FromFloor.UpCall) ||
               (CurrentDirection == Direction.Down && FromFloor.DownCall) ||
               (State == ElevatorState.Idle && (FromFloor.UpCall || FromFloor.DownCall)))
            {
                return true;
            }
            return false;
        }
    }
}

