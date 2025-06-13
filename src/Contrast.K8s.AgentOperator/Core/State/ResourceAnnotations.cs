// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Contrast.K8s.AgentOperator.Core.State
{
    public class ResourceAnnotations
    {
        public const string ResourceManagedByAttributeName = "agents.contrastsecurity.com/managed-by";

        public static Dictionary<string, string> GetAnnotationsForManagedResources(string resourceName, string resourceNamespace)
        {
            return new Dictionary<string, string>
            {
                { ResourceManagedByAttributeName, $"{resourceNamespace}/{resourceName}" },
            };
        }


    }
}
