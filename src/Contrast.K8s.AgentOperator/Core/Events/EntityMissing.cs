using k8s;
using k8s.Models;
using MediatR;

namespace Contrast.K8s.AgentOperator.Core.Events
{
    public record EntityMissing<T>(string Name, string Namespace) : IRequest where T : IKubernetesObject<V1ObjectMeta>;
}
