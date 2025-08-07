using Elevators.Core.Interfaces;
using Serilog;

namespace Elevators.Core.Models
{
    public class Floor(int floorNumber, ILogger logger) : IFloor
    {
        public int FloorNumber { get; private set; } = floorNumber;
        public List<Passenger> Passengers { get; private set; } = [];
        public bool UpCall { get; set; } = false;
        public bool DownCall { get; set; } = false;
        private readonly ILogger _logger = logger;

        public void AddPassenger(Passenger passenger)
        {
            Passengers.Add(passenger);
            if (passenger.ToFloor > FloorNumber)
            {
                UpCall = true;
            }
            else if (passenger.ToFloor < FloorNumber)
            {
                DownCall = true;
            }
            _logger.Information("Passenger {PassengerId} arrived at Floor {FloorNumber} and wants to go to {ToFloor}.", passenger.Id, FloorNumber, passenger.ToFloor);
        }

        public void ClearCalls()
        {
            UpCall = false;
            DownCall = false;
        }

        public override string ToString()
        {
            return $"Floor {FloorNumber} (Waiting: {Passengers.Count}, Up Call: {UpCall}, Down Call: {DownCall})";
        }
    }

}
