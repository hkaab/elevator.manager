namespace Elevators.Core.Models.Enums
{
    public enum ElevatorCommand
    {
        CallRequest,          // Request to call an elevator
        SummonGeneralElevator,     // General passenger request
        SummonPrivateElevator,      // Request for a private elevator
        SummonServiceElevator,      // Request for a service elevator
        EmergencyCall,        // Emergency call request
        FireAlarm,            // Fire alarm activation
        SetIssue              // Set an issue with an elevator
    }
}
