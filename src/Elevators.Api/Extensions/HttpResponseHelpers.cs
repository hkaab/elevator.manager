using Newtonsoft.Json;
using System.Net;

namespace Elevators.Api.Extensions
{
    public static class HttpResponseHelpers
    {
        public static async Task WriteJsonResponse(this HttpResponse response, int statusCode, object value)
        {
            if (!response.HasStarted)
            {
                response.StatusCode = statusCode;
                response.ContentType = "application/json";
                await response.WriteAsync(JsonConvert.SerializeObject(value));
            }
        }

        public static Task WriteJsonResponse(this HttpResponse response, HttpStatusCode statusCode, object value)
        {
            return response.WriteJsonResponse((int)statusCode, value);
        }
    }
}