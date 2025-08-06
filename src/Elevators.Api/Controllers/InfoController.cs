using Elevators.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement;
using Swashbuckle.AspNetCore.Annotations;
using System.Net.Mime;
using Elevators.Core.Extensions;

namespace Elevators.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InfoController : Controller
    {
        private readonly IFeatureManager _featureManager;

        public InfoController(IFeatureManager featureManager)
        {
           _featureManager = featureManager;

        }
        [HttpGet("featureflags")]
        [SwaggerOperation(Summary = "Get feature flags set in an environment", Description = "Get feature flags set in an environment", OperationId = "Info_FeatureFlag_GET")]
        [Produces(MediaTypeNames.Application.Json)]
        [SwaggerResponse(StatusCodes.Status200OK, type: typeof(FeatureFlagsInfo))]
        public async Task<ActionResult<FeatureFlagsInfo>> GetFeatureFlags(CancellationToken cancellationToken)
        {
            var features = await _featureManager.FeatureFlags(cancellationToken);
            var info = new FeatureFlagsInfo
            {
                FeatureFlags = features
            };

            return Ok(info);
        }
    }
}
