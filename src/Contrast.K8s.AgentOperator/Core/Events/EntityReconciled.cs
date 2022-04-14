using k8s;
using k8s.Models;
using MediatR;

namespace Contrast.K8s.AgentOperator.Core.Events
{
    public record EntityReconciled<T>(T Entity) : IRequest where T : IKubernetesObject<V1ObjectMeta>;
}
