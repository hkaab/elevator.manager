using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Elevators.api.Controllers
{
    [ApiController]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class ErrorController : ControllerBase
    {
        private readonly ILogger<ErrorController> _logger;

        public ErrorController(ILogger<ErrorController> logger)
        {
            _logger = logger;
        }

        [Route("/error")]
        public IActionResult HandleError()
        {
            var exceptionHandlerPathFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            var exception = exceptionHandlerPathFeature?.Error;

            _logger.LogError(exception, "An unhandled exception occurred during request processing.");

            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred.",
                Detail = "Please try again later. If the problem persists, contact support.",
                Instance = HttpContext.Request.Path
            };

            return StatusCode(StatusCodes.Status500InternalServerError, problemDetails);
        }
    }
}
