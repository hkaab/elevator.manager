using Microsoft.FeatureManagement;

namespace Elevators.Core.Extensions;
public static class FeatureFlagsExtensions
{
    public  static async Task<Dictionary<string,bool>> FeatureFlags(this IFeatureManager value, CancellationToken cancellationToken)
    {
        var features = new Dictionary<string, bool>();
        
        await foreach (var item in value.GetFeatureNamesAsync())
        {
            var isEnabled = await value.IsEnabledAsync(item, cancellationToken);
            features.Add(item, isEnabled);
        }

        return features;
    }
}