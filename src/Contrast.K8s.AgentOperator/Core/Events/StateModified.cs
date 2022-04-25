using Contrast.K8s.AgentOperator.Core.State.Resources.Interfaces;
using k8s;
using k8s.Models;
using MediatR;

namespace Contrast.K8s.AgentOperator.Core.Events
{
    public record StateModified<TResource, TKubernetesObject>(TResource? Previous,
                                                              TResource? Current,
                                                              TKubernetesObject RelevantKubernetesObject)
        : INotification
        where TResource : INamespacedResource
        where TKubernetesObject : IKubernetesObject<V1ObjectMeta>;

    public class StateModified
    {
        public static StateModified<TResource, TKubernetesObject> Create<TResource, TKubernetesObject>(TResource? previous,
                                                                                                       TResource? current,
                                                                                                       TKubernetesObject relevantKubernetesObject)
            where TResource : INamespacedResource
            where TKubernetesObject : IKubernetesObject<V1ObjectMeta>
        {
            return new StateModified<TResource, TKubernetesObject>(previous, current, relevantKubernetesObject);
        }
    }
}
