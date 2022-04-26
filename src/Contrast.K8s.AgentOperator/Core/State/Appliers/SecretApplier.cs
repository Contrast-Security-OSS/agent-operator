using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.State.Resources;
using JetBrains.Annotations;
using k8s.Models;
using MediatR;

namespace Contrast.K8s.AgentOperator.Core.State.Appliers
{
    [UsedImplicitly]
    public class SecretApplier : BaseApplier<V1Secret, SecretResource>
    {
        public SecretApplier(IStateContainer stateContainer, IMediator mediator) : base(stateContainer, mediator)
        {
        }

        protected override ValueTask<SecretResource> CreateFrom(V1Secret entity, CancellationToken cancellationToken = default)
        {
            var resource = new SecretResource(
                entity.Data.Keys.ToList()
            );
            return ValueTask.FromResult(resource);
        }
    }
}
