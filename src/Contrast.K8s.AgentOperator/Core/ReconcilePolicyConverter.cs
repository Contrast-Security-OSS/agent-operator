// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Contrast.K8s.AgentOperator.Core.State.Resources.Primitives;

namespace Contrast.K8s.AgentOperator.Core;

public static class ReconcilePolicyConverter
{
    public static ReconcilePolicy GetPolicyFromString(string? value)
    {
        return value?.ToLowerInvariant() switch
        {
            "oncreate" => ReconcilePolicy.OnCreate,
            _ => ReconcilePolicy.Always
        };
    }

    public static string GetStringFromPolicy(ReconcilePolicy policy)
    {
        return policy switch
        {
            ReconcilePolicy.OnCreate => "OnCreate",
            _ => "Always"
        };
    }
}
