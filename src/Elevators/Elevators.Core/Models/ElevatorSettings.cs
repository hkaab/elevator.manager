namespace Elevators.Core.Models
{
    public class ElevatorSettings
    {
        public int NumberOfPublicElevators { get; set; }
        public int NumberOfPrivateElevators { get; set; }
        public int NumberOfServiceElevators { get; set; }
        public int MaxFloors { get; set; }
        public int ElevatorCapacity { get; set; }
        public bool PublicElevatorHasMusic { get; set; }
        public bool PublicElevatorHasSpeaker { get; set; }
        public bool PrivateElevatorHasMusic { get; set; }
        public bool PrivateElevatorHasSpeaker { get; set; }
        public bool ServiceElevatorHasMusic { get; set; }
        public bool ServiceElevatorHasSpeaker { get; set; }
    }
}
