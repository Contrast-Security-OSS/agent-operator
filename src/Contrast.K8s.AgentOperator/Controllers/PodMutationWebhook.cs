// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.Events;
using Contrast.K8s.AgentOperator.Core.Kube;
using JetBrains.Annotations;
using k8s.Models;
using KubeOps.Operator.Rbac;
using KubeOps.Operator.Webhooks;
using MediatR;

namespace Contrast.K8s.AgentOperator.Controllers
{
    [EntityRbac(typeof(V1Pod), Verbs = VerbConstants.ReadAndPatch), UsedImplicitly]
    public class PodMutationWebhook : IMutationWebhook<V1Pod>
    {
        private readonly IMediator _mediator;

        public AdmissionOperations Operations => AdmissionOperations.Create;

        public PodMutationWebhook(IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task<MutationResult> CreateAsync(V1Pod newEntity, bool dryRun)
        {
            var result = await _mediator.Send(new EntityCreating<V1Pod>(newEntity));

            return result is NeedsChangeEntityCreatingMutationResult<V1Pod> mutationResult
                ? MutationResult.Modified(mutationResult.Entity)
                : MutationResult.NoChanges();
        }
    }
}
