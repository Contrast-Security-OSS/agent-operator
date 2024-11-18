// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.Events;
using Contrast.K8s.AgentOperator.Core.Kube;
using JetBrains.Annotations;
using k8s.Models;
using KubeOps.Abstractions.Rbac;
using KubeOps.Operator.Web.Webhooks.Admission.Mutation;
using MediatR;

namespace Contrast.K8s.AgentOperator.Controllers;

[EntityRbac(typeof(V1Pod), Verbs = VerbConstants.ReadAndPatch), UsedImplicitly]
[MutationWebhook(typeof(V1Pod))]
public class PodMutationWebhook : MutationWebhook<V1Pod>
{
    private readonly IMediator _mediator;


    public PodMutationWebhook(IMediator mediator)
    {
        _mediator = mediator;
    }
    public override async Task<MutationResult<V1Pod>> CreateAsync(
        V1Pod newEntity,
        bool dryRun,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new EntityCreating<V1Pod>(newEntity), cancellationToken);

        return result is NeedsChangeEntityCreatingMutationResult<V1Pod> mutationResult
            ? Modified(mutationResult.Entity)
            : NoChanges();
    }
}
