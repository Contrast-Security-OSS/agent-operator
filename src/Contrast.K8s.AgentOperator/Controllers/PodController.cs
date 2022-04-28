using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core;
using Contrast.K8s.AgentOperator.Core.Events;
using JetBrains.Annotations;
using k8s.Models;
using KubeOps.Operator.Rbac;
using KubeOps.Operator.Webhooks;
using MediatR;
using NLog;

namespace Contrast.K8s.AgentOperator.Controllers
{
    [EntityRbac(typeof(V1Pod), Verbs = VerbConstants.ReadAndPatch), UsedImplicitly]
    public class PodController : IMutationWebhook<V1Pod>
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly IMediator _mediator;

        public AdmissionOperations Operations => AdmissionOperations.Create;

        public PodController(IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task<MutationResult> CreateAsync(V1Pod newEntity, bool dryRun)
        {
            var result = await _mediator.Send(new EntityCreating<V1Pod>(newEntity));

            if (result is NeedsChangeEntityCreatingMutationResult<V1Pod> mutationResult)
            {
                Logger.Info($"Modifying pod '{newEntity.Namespace()}/{newEntity.Name()}'.");
                return MutationResult.Modified(mutationResult.Entity);
            }
            else
            {
                Logger.Info($"No changes required for pod '{newEntity.Namespace()}/{newEntity.Name()}'.");
                return MutationResult.NoChanges();
            }
        }
    }
}
