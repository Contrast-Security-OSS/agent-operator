using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.Events;
using Contrast.K8s.AgentOperator.Core.Kube;
using Contrast.K8s.AgentOperator.Core.State;
using Contrast.K8s.AgentOperator.Core.State.Resources;
using Contrast.K8s.AgentOperator.Core.State.Resources.Interfaces;
using JetBrains.Annotations;
using k8s.Models;
using MediatR;

namespace Contrast.K8s.AgentOperator.Core.Injecting
{
    [UsedImplicitly]
    public class ApplyDesiredStateHandler : INotificationHandler<InjectorMatched>
    {
        private readonly IResourceHasher _hasher;
        private readonly IStateContainer _state;
        private readonly IResourcePatcher _patcher;

        public ApplyDesiredStateHandler(IResourceHasher hasher, IStateContainer state, IResourcePatcher patcher)
        {
            _hasher = hasher;
            _state = state;
            _patcher = patcher;
        }

        public async Task Handle(InjectorMatched notification, CancellationToken cancellationToken)
        {
            var (target, injector) = notification;
            var desiredState = await GetDesiredState(injector, cancellationToken);

            var templateAnnotations = target.Resource.PodTemplate.Annotations;
            if ((templateAnnotations.GetAnnotation(InjectionConstants.HashAttributeName) != desiredState.Hash)
                || (templateAnnotations.GetAnnotation(InjectionConstants.NameAttributeName) != desiredState.Name)
                || (templateAnnotations.GetAnnotation(InjectionConstants.NamespaceAttributeName) != desiredState.Namespace))
            {
                await PatchToDesiredState(desiredState, target);
            }
        }

        private async Task<DesiredState> GetDesiredState(ResourceIdentityPair<AgentInjectorResource>? injector, CancellationToken cancellationToken = default)
        {
            if (injector != null
                && await _state.GetInjectorBundle(injector.Identity.Name, injector.Identity.Namespace, cancellationToken)
                    is var (_, connection, configuration, secrets))
            {
                var hash = _hasher.GetHash(injector.Resource, connection, configuration, secrets);
                return new DesiredState(hash, injector.Identity.Name, injector.Identity.Namespace);
            }

            return DesiredState.Empty;
        }

        private ValueTask PatchToDesiredState(DesiredState desiredState, ResourceIdentityPair<IResourceWithPodTemplate> target)

        {
            var (identity, podTemplate) = target;
            return podTemplate switch
            {
                DaemonSetResource => PatchToDesiredStateDaemonSet(desiredState, identity),
                StatefulSetResource => PatchToDesiredStateStatefulSet(desiredState, identity),
                DeploymentResource => PatchToDesiredStateDeployment(desiredState, identity),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private async ValueTask PatchToDesiredStateDaemonSet(DesiredState desiredState, NamespacedResourceIdentity identity)
        {
            await _state.MarkAsDirty(identity);
            await _patcher.Patch<V1DaemonSet>(identity.Name, identity.Namespace, o => { PatchAnnotations(desiredState, o.Spec.Template); });
        }

        private async ValueTask PatchToDesiredStateStatefulSet(DesiredState desiredState, NamespacedResourceIdentity identity)
        {
            await _state.MarkAsDirty(identity);
            await _patcher.Patch<V1StatefulSet>(identity.Name, identity.Namespace, o => { PatchAnnotations(desiredState, o.Spec.Template); });
        }

        private async ValueTask PatchToDesiredStateDeployment(DesiredState desiredState, NamespacedResourceIdentity identity)
        {
            await _state.MarkAsDirty(identity);
            await _patcher.Patch<V1Deployment>(identity.Name, identity.Namespace, o => { PatchAnnotations(desiredState, o.Spec.Template); });
        }

        private static void PatchAnnotations(DesiredState desiredState, V1PodTemplateSpec spec)
        {
            var annotations = spec.Metadata.EnsureAnnotations();
            annotations.RemovePrefixed(InjectionConstants.OperatorAttributePrefix);
            annotations.SetOrRemove(InjectionConstants.HashAttributeName, desiredState.Hash);
            annotations.SetOrRemove(InjectionConstants.NameAttributeName, desiredState.Name);
            annotations.SetOrRemove(InjectionConstants.NamespaceAttributeName, desiredState.Namespace);
            if (!spec.Metadata.Annotations.Any())
            {
                spec.Metadata.Annotations = null;
            }
        }

        private record DesiredState(string? Hash, string? Name, string? Namespace)
        {
            public static DesiredState Empty { get; } = new(null, null, null);
        }
    }
}
