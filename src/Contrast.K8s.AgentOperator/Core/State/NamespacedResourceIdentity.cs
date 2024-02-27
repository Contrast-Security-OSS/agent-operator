// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using Contrast.K8s.AgentOperator.Core.State.Resources.Interfaces;
using JetBrains.Annotations;

namespace Contrast.K8s.AgentOperator.Core.State;

public record NamespacedResourceIdentity([UsedImplicitly] string Name, [UsedImplicitly] string Namespace, [UsedImplicitly] Type Type)
{
    public static NamespacedResourceIdentity Create<T>(string name, string @namespace) where T : INamespacedResource
    {
        return new NamespacedResourceIdentity(name, @namespace, typeof(T));
    }

    public override string ToString()
    {
        return $"{Type.Name}/{Namespace}/{Name}";
    }
}
