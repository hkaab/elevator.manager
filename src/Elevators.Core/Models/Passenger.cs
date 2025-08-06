namespace Elevators.Core.Models
{
    public class Passenger(int id, int currentFloor, int destinationFloor, bool hasSwappedCard = false)
    {
        public int Id { get; private set; } = id;
        public int CurrentFloor { get; private set; } = currentFloor;
        public int DestinationFloor { get; private set; } = destinationFloor;
        public bool IsInsideElevator { get; set; } = false;
        public bool HasSwappedCard { get; private set; } = hasSwappedCard;

        public override string ToString()
        {
            return $"P-{Id} (From: {CurrentFloor}, To: {DestinationFloor})";
        }
    }
}
