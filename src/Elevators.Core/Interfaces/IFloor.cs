using Elevators.Core.Models;

namespace Elevators.Core.Interfaces
{
    public interface IFloor
    {
        int FloorNumber { get; }
        List<Passenger> Passengers { get; }
        bool UpCall { get; set; }
        bool DownCall { get; set; }
        void AddPassenger(Passenger passenger);
        void ClearCalls();
    }
}
