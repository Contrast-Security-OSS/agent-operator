// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Contrast.K8s.AgentOperator.Core.State.Resources.Primitives;

namespace Contrast.K8s.AgentOperator.Core;

public static class ReconcilePolicyConverter
{
    // Only 'OnCreate' is a meaningful policy value. Unset and 'Always' both map to null so that
    // the default injector serializes without a reconcilePolicy property, keeping its hash identical
    // to operator versions that predate this field. Otherwise every existing workload would see a
    // changed injector-hash on upgrade and be re-patched, forcing a cluster-wide rolling restart.
    public static ReconcilePolicy? GetPolicyFromString(string? value)
    {
        return value?.ToLowerInvariant() switch
        {
            "oncreate" => ReconcilePolicy.OnCreate,
            _ => null
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
