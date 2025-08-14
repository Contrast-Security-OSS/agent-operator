// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Contrast.K8s.AgentOperator.Core.State.Resources.Interfaces;

public interface IClusterResource : INamespacedResource
{
    public IReadOnlyCollection<string> NamespacePatterns { get; }
}
