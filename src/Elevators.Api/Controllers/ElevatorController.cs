using Elevators.Core.Constants;
using Elevators.Core.Interfaces;
using Elevators.Core.Models;
using Elevators.Core.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;

namespace Elevators.api.Controllers
{
    [ApiController]
    [Route(Endpoints.Elevator)]
    public class ElevatorController : ControllerBase
    {
        private readonly IElevatorManagerService _elevatorService;
        private readonly ILogger<ElevatorController> _logger;

        public ElevatorController(IElevatorManagerService elevatorService, ILogger<ElevatorController> logger)
        {
            _elevatorService = elevatorService;
            _logger = logger;
        }

        [HttpGet("status")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Produces(MediaTypeNames.Application.Json)]
        public IActionResult GetStatus()
        {
            try
            {
                var status = new
                {
                    Elevators = _elevatorService.Elevators.Select(e => new
                    {
                        e.Id,
                        type= e.Type.ToString(),
                        e.CurrentFloor,
                        state = e.State.ToString(),
                        direction= e.CurrentDirection.ToString(),
                        Passengers = e.Passengers.Select(p => p.ToString()),
                        e.SummonRequests
                    }),

                    Floors = _elevatorService.Floors.Select(f => new
                    {
                        f.Value.FloorNumber,
                        Passengers = f.Value.Passengers.Select(p => p.ToString()),
                        f.Value.UpCall,
                        f.Value.DownCall
                    }),
                    _elevatorService.FireAlarmActive
                };
                return Ok(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving system status.");
                return StatusCode(500, "An internal server error occurred.");
            }
        }

        [HttpPost("request/general")]
        [ProducesResponseType(StatusCodes.Status202Accepted)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<IActionResult> AddGeneralRequest([FromBody] GeneralRequest request)
        {
            try
            {
                if (request == null || request.FromFloor < 0 || request.ToFloor < 0)
                {
                    return BadRequest("Invalid request data.");
                }
                if (request.FromFloor == request.ToFloor)
                {
                    return BadRequest("Current floor and destination floor cannot be the same.");
                }

                ElevatorCommandRequest commandRequest = new ()
                {
                    ElevatorCommand = ElevatorCommand.SummonGeneralElevator,
                    FromFloor = request.FromFloor,
                    ToFloor = request.ToFloor
                };

                await _elevatorService.QueueElevatorCommandRequest(commandRequest);

                return Accepted();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while adding a general request.");
                return StatusCode(500, "An internal server error occurred.");
            }
        }

        [HttpPost("request/private/{elevatorId}")]
        [ProducesResponseType(StatusCodes.Status202Accepted)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<IActionResult> AddPrivateRequest(int elevatorId, [FromBody] GeneralRequest request)
        {
            try
            {
                if (request == null || request.FromFloor < 0 || request.ToFloor < 0)
                {
                    return BadRequest("Invalid request data.");
                }
                if (request.FromFloor == request.ToFloor)
                {
                    return BadRequest("Current floor and destination floor cannot be the same.");
                }
                if (!_elevatorService.Elevators.Any(e => e.Id == elevatorId))
                {
                    return NotFound($"Elevator with ID {elevatorId} not found.");
                }
                ElevatorCommandRequest commandRequest = new ()
                {
                    ElevatorCommand = ElevatorCommand.SummonPrivateElevator,
                    ElevatorId = elevatorId,
                    FromFloor = request.FromFloor,
                    ToFloor = request.ToFloor
                };  
                await _elevatorService.QueueElevatorCommandRequest(commandRequest);
                return Accepted();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while adding a private request for elevator {ElevatorId}.", elevatorId);
                return StatusCode(500, "An internal server error occurred.");
            }
        }

        [HttpPost("request/service")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<IActionResult> AddServiceRequest([FromBody] ServiceRequest request)
        {
            try
            {
                if (request == null || request.FromFloor < 0 || request.ToFloor < 0)
                {
                    return BadRequest("Invalid request data.");
                }
                if (request.FromFloor == request.ToFloor)
                {
                    return BadRequest("Current floor and destination floor cannot be the same.");
                }
                if (!request.HasSwappedCard)
                {
                    return BadRequest("Swapping cards is required");
                }
                ElevatorCommandRequest commandRequest = new ()
                {
                    ElevatorCommand = ElevatorCommand.SummonServiceElevator,
                    FromFloor = request.FromFloor,
                    ToFloor = request.ToFloor,
                    HasSwappedCard = request.HasSwappedCard

                };
                await _elevatorService.QueueElevatorCommandRequest(commandRequest);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while adding a service request.");
                return StatusCode(500, "An internal server error occurred.");
            }
        }

        [HttpPost("firealarm/{active}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<IActionResult> SetFireAlarm(bool active)
        {
            try
            {
                if (active && _elevatorService.FireAlarmActive)
                {
                    return BadRequest("Fire alarm is already active.");
                }
                if (!active && !_elevatorService.FireAlarmActive)
                {
                    return BadRequest("Fire alarm is already inactive.");
                }
                _logger.LogInformation("Setting fire alarm state to {Active}", active);
                ElevatorCommandRequest commandRequest = new ()
                {
                    ElevatorCommand = ElevatorCommand.FireAlarm,
                    FireAlarmActive = active
                };

                await _elevatorService.QueueElevatorCommandRequest(commandRequest);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while setting the fire alarm state.");
                return StatusCode(500, "An internal server error occurred.");
            }
        }

        [HttpPost("issue/{elevatorId}/{hasIssue}")]
        [ProducesResponseType(StatusCodes.Status202Accepted)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<IActionResult> SetElevatorIssue(int elevatorId, bool hasIssue)
        {
            try
            {
                if (!_elevatorService.Elevators.Any(e => e.Id == elevatorId))
                {
                    return NotFound($"Elevator with ID {elevatorId} not found.");
                }
                if (hasIssue && _elevatorService.Elevators.First(e => e.Id == elevatorId).HasMechanicalIssue)
                {
                    return BadRequest("Elevator already has a mechanical issue.");
                }   
                if (!hasIssue && !_elevatorService.Elevators.First(e => e.Id == elevatorId).HasMechanicalIssue)
                {
                    return BadRequest("Elevator does not have a mechanical issue to resolve.");
                }
                _logger.LogInformation("Setting elevator {ElevatorId} issue state to {HasIssue}", elevatorId, hasIssue);
                ElevatorCommandRequest commandRequest = new ()
                {
                    ElevatorCommand = ElevatorCommand.SetIssue,
                    ElevatorId = elevatorId,
                    HasIssue = hasIssue
                };
                await _elevatorService.QueueElevatorCommandRequest(commandRequest);
                return Accepted();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while setting an issue for elevator {ElevatorId}.", elevatorId);
                return StatusCode(500, "An internal server error occurred.");
            }
        }

        [HttpPost("emergencycall/{elevatorId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<IActionResult> EmergencyCall(int elevatorId)
        {
            try
            {
                if (!_elevatorService.Elevators.Any(e => e.Id == elevatorId))
                {
                    return NotFound($"Elevator with ID {elevatorId} not found.");
                }
                if (_elevatorService.Elevators.First(e => e.Id == elevatorId).IsEmergencyCallActive)
                {
                    return BadRequest("Emergency call is already active for this elevator.");
                }
                _logger.LogInformation("Activating emergency call for elevator {ElevatorId}", elevatorId);

                ElevatorCommandRequest commandRequest = new()
                {
                    ElevatorCommand = ElevatorCommand.EmergencyCall,
                    ElevatorId = elevatorId
                };

                await _elevatorService.QueueElevatorCommandRequest(commandRequest);

                return Accepted();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while activating the emergency call for elevator {ElevatorId}.", elevatorId);
                return StatusCode(500, "An internal server error occurred.");
            }
        }
    }
}
