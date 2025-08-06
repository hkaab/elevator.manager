using System.Net;

namespace Elevators.Api.Middleware
{
    public class HealthCheckMiddleware
    {
        private readonly RequestDelegate _next;

        public HealthCheckMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            if (httpContext.Request.Path.Equals("/health"))
            {
                httpContext.Response.StatusCode = (int)HttpStatusCode.OK;
            }
            else
            {
                await _next(httpContext);
            }
        }
    }
}
