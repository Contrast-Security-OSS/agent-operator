// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Contrast.K8s.AgentOperator.Core.State.Resources.Interfaces;
using MediatR;

namespace Contrast.K8s.AgentOperator.Core.Events
{
    public record StateModified<TResource>(TResource? Previous, TResource? Current) : StateModified where TResource : INamespacedResource;

    public record StateModified : INotification
    {
        public static StateModified<TResource> Create<TResource>(TResource? previous,
                                                                 TResource? current)
            where TResource : INamespacedResource
        {
            return new StateModified<TResource>(previous, current);
        }
    }

    public record DeferredStateModified(int MergedChanges) : INotification
    {
        public static DeferredStateModified FirstMerged { get; } = new(1);
    }
}
