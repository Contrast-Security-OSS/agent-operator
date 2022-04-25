using Contrast.K8s.AgentOperator.Core.State.Resources.Interfaces;
using MediatR;

namespace Contrast.K8s.AgentOperator.Core.Events
{
    public record StateModified<TResource>(TResource? Previous, TResource? Current)
        : INotification
        where TResource : INamespacedResource;

    public class StateModified
    {
        public static StateModified<TResource> Create<TResource>(TResource? previous,
                                                                 TResource? current)
            where TResource : INamespacedResource
        {
            return new StateModified<TResource>(previous, current);
        }
    }
}
