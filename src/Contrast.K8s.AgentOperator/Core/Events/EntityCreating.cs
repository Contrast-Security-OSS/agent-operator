// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using k8s;
using k8s.Models;
using MediatR;

namespace Contrast.K8s.AgentOperator.Core.Events
{
    public record EntityCreating<T>(T Entity) : IRequest<EntityCreatingMutationResult<T>> where T : IKubernetesObject<V1ObjectMeta>;

    public abstract record EntityCreatingMutationResult<T>
        where T : IKubernetesObject<V1ObjectMeta>
    {
        public static NoChangeEntityCreatingMutationResult<T> NoChange { get; } = new();
    }

    public record NoChangeEntityCreatingMutationResult<T> : EntityCreatingMutationResult<T>
        where T : IKubernetesObject<V1ObjectMeta>;

    public record NeedsChangeEntityCreatingMutationResult<T>(T Entity) : EntityCreatingMutationResult<T>
        where T : IKubernetesObject<V1ObjectMeta>;
}
