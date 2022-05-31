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
            if (request.Entity.Metadata.Annotations != null
                && request.Entity.Metadata.Annotations.TryGetValue(InjectionConstants.NameAttributeName, out var injectorName)
                && request.Entity.Metadata.Annotations.TryGetValue(InjectionConstants.NamespaceAttributeName, out var injectorNamespace)
                && await _state.GetInjectorBundle(injectorName, injectorNamespace, cancellationToken)
                    is var (injector, connection, configuration, _))
            {
                var context = new PatchingContext(injector, connection, configuration, "/contrast");
                await _patcher.Patch(context, request.Entity, cancellationToken);

                Logger.Trace($"Patching pod using injector '{injectorNamespace}/{injectorName}'.");
                return new NeedsChangeEntityCreatingMutationResult<V1Pod>(request.Entity);
            }

            Logger.Trace("Ignored pod creation, not selected by any known agent injectors.");
            return new NoChangeEntityCreatingMutationResult<V1Pod>();
        }
    }
}
