using Elevators.Core.Models.Enums;

namespace Elevators.Core.Models
{
    public class ElevatorCommandRequest
    {
        public ElevatorCommandRequest()
        {
            RequestTime = DateTime.UtcNow;
        }
        public int Id { get; set; }
        public required ElevatorCommand ElevatorCommand { get; set; }

        public int? FromFloor { get; set; }

        public int? ToFloor { get; set; }

        public int ElevatorId { get; set; }
        public DateTime RequestTime { get; set; } 
        public bool HasSwappedCard { get; set; }
        public bool FireAlarmActive { get; set; }
        public bool HasIssue { get; set; }
        public override string ToString()
        {
            return $"ElevatorCommandRequest: Id={Id}, Command={ElevatorCommand}, FromFloor={FromFloor}, ToFloor={ToFloor}, ElevatorId={ElevatorId}, RequestTime={RequestTime}, HasSwappedCard={HasSwappedCard}, FireAlarmActive={FireAlarmActive}";
        }

    }
}
