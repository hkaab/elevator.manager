namespace Elevators.Core.Models
{
    public class ServiceRequest
    {
        public int CurrentFloor { get; set; }
        public int DestinationFloor { get; set; }
        public bool HasSwappedCard { get; set; }
    }
}
