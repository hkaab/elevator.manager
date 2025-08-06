using Elevators.Api.Extensions;
using System.Net;


namespace Elevators.Api.Middleware
{
    public class ErrorHandlingMiddleware
    {
        public const int ServiceDependencyFailure = 513;

        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger;

        public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                await httpContext.Response.WriteJsonResponse(HttpStatusCode.InternalServerError, BuildErrorMessage(ex));
            }
        }

        private object BuildErrorMessage(Exception ex)
        {
            return new { Message = $"Internal Error: {ex.ToString()} : {ex.Message} {ex.InnerException}" };
        }
    }
}