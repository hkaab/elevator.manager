using Elevators.Core.Interfaces;
using Elevators.Core.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;

namespace Elevators.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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
                        e.Type,
                        e.CurrentFloor,
                        e.State,
                        e.CurrentDirection,
                        Passengers = e.Passengers.Select(p => p.ToString()),
                        e.SummonRequests
                    }),

                    Floors = _elevatorService.Floors.Select(f => new
                    {
                        f.FloorNumber,
                        Passengers = f.Passengers.Select(p => p.ToString()),
                        f.UpCall,
                        f.DownCall
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
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<IActionResult> AddGeneralRequest([FromBody] GeneralRequest request)
        {
            try
            {
                await _elevatorService.AddGeneralPassengerRequest(request.CurrentFloor, request.DestinationFloor);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while adding a general request.");
                return StatusCode(500, "An internal server error occurred.");
            }
        }

        [HttpPost("request/private/{elevatorId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<IActionResult> AddPrivateRequest(int elevatorId, [FromBody] GeneralRequest request)
        {
            try
            {
                await _elevatorService.AddPrivateElevatorRequest(elevatorId, request.CurrentFloor, request.DestinationFloor);
                return Ok();
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
                await _elevatorService.AddServiceElevatorRequest(request.CurrentFloor, request.DestinationFloor, request.HasSwappedCard);
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
        public IActionResult SetFireAlarm(bool active)
        {
            try
            {
                _elevatorService.SetFireAlarm(active);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while setting the fire alarm state.");
                return StatusCode(500, "An internal server error occurred.");
            }
        }

        [HttpPost("issue/{elevatorId}/{hasIssue}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Produces(MediaTypeNames.Application.Json)]
        public IActionResult SetElevatorIssue(int elevatorId, bool hasIssue)
        {
            try
            {
                _elevatorService.SetElevatorIssue(elevatorId, hasIssue);
                return Ok();
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
                await _elevatorService.EmergencyCallAsync(elevatorId);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while activating the emergency call for elevator {ElevatorId}.", elevatorId);
                return StatusCode(500, "An internal server error occurred.");
            }
        }
    }
}
