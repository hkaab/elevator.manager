namespace Elevators.Core.Models
{
    public class Passenger(int id, int FromFloor, int ToFloor, bool hasSwappedCard = false)
    {
        public int Id { get; private set; } = id;
        public int FromFloor { get; private set; } = FromFloor;
        public int ToFloor { get; private set; } = ToFloor;
        public bool IsInsideElevator { get; set; } = false;
        public bool HasSwappedCard { get; private set; } = hasSwappedCard;

        public override string ToString()
        {
            return $"P-{Id} (From: {FromFloor}, To: {ToFloor})";
        }
    }
}
