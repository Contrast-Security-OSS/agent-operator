// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using k8s.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Contrast.K8s.AgentOperator.Core.Reactions.Injecting.Patching.Utility;

public static class PatchingExtensions
{
    public static V1EnvVar? FirstOrDefault(this IEnumerable<V1EnvVar> collection, string name)
    {
        return collection.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
    }
}
