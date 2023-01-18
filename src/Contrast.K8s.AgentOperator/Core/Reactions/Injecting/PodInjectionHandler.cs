// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.Events;
using Contrast.K8s.AgentOperator.Core.Reactions.Injecting.Patching;
using Contrast.K8s.AgentOperator.Core.State;
using JetBrains.Annotations;
using k8s.Models;
using MediatR;
using NLog;

namespace Contrast.K8s.AgentOperator.Core.Reactions.Injecting
{
    [UsedImplicitly]
    public class PodInjectionHandler : IRequestHandler<EntityCreating<V1Pod>, EntityCreatingMutationResult<V1Pod>>
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly IStateContainer _state;
        private readonly IPodPatcher _patcher;

        public PodInjectionHandler(IStateContainer state, IPodPatcher patcher)
        {
            _state = state;
            _patcher = patcher;
        }

        public async Task<EntityCreatingMutationResult<V1Pod>> Handle(EntityCreating<V1Pod> request, CancellationToken cancellationToken)
        {
            if (request.Entity.Metadata.Annotations is { } annotations
                && annotations.TryGetValue(InjectionConstants.InjectorNameAttributeName, out var injectorName)
                && annotations.TryGetValue(InjectionConstants.InjectorNamespaceAttributeName, out var injectorNamespace)
                && annotations.TryGetValue(InjectionConstants.WorkloadNameAttributeName, out var workloadName)
                && annotations.TryGetValue(InjectionConstants.WorkloadNamespaceAttributeName, out var workloadNamespace)
                && await _state.GetInjectorBundle(injectorName, injectorNamespace, cancellationToken)
                    is var (injector, connection, configuration, _))
            {
                var context = new PatchingContext(
                    workloadName,
                    workloadNamespace,
                    injector,
                    connection,
                    configuration,
                    "/contrast/agent",
                    "/contrast/data"
                );
                await _patcher.Patch(context, request.Entity, cancellationToken);

                Logger.Info("Patching pod "
                            + $"from '{workloadNamespace}/{workloadName}' "
                            + $"using injector '{injectorNamespace}/{injectorName}'.");
                return new NeedsChangeEntityCreatingMutationResult<V1Pod>(request.Entity);
            }

            Logger.Debug("Ignored pod creation, the pod was not selected by any known agent injectors.");
            return EntityCreatingMutationResult<V1Pod>.NoChange;
        }
    }
}
