using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.Events;
using Contrast.K8s.AgentOperator.Core.Kube;
using Contrast.K8s.AgentOperator.Core.State;
using Contrast.K8s.AgentOperator.Core.State.Resources;
using Contrast.K8s.AgentOperator.Core.State.Resources.Interfaces;
using DotnetKubernetesClient;
using JetBrains.Annotations;
using k8s.Models;
using MediatR;

namespace Contrast.K8s.AgentOperator.Core.Injecting
{
    [UsedImplicitly]
    public class ApplyDesiredStateHandler : INotificationHandler<InjectorMatched>
    {
        private readonly IInjectorHasher _hasher;
        private readonly IStateContainer _state;
        private readonly IKubernetesClient _client;
        private readonly IResourcePatcher _patcher;

        public ApplyDesiredStateHandler(IInjectorHasher hasher, IStateContainer state, IKubernetesClient client, IResourcePatcher patcher)
        {
            _hasher = hasher;
            _state = state;
            _client = client;
            _patcher = patcher;
        }

        public async Task Handle(InjectorMatched notification, CancellationToken cancellationToken)
        {
            var (target, injector) = notification;

            if (await _state.GetIsDirty(target.Identity, cancellationToken))
            {
                return;
            }

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
                    is var (_, connection, configuration))
            {
                var hash = _hasher.GetHash(injector.Resource, connection, configuration);
                return new DesiredState(hash, injector.Identity.Name, injector.Identity.Namespace);
            }

            return DesiredState.Empty;
        }

        private ValueTask PatchToDesiredState(DesiredState desiredState, ResourceIdentityPair<IResourceWithPodTemplate> target)

        {
            var (identity, podTemplate) = target;
            return podTemplate switch
            {
                DaemonSetResource => PatchToDesiredStateDaemonSetResource(desiredState, identity),
                StatefulSetResource => PatchToDesiredStateStatefulSetResource(desiredState, identity),
                DeploymentResource => PatchToDesiredStateDeployment(desiredState, identity),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private async ValueTask PatchToDesiredStateDaemonSetResource(DesiredState desiredState, NamespacedResourceIdentity identity)
        {
            var existingEntity = await _client.Get<V1DaemonSet>(identity.Name, identity.Namespace);
            if (existingEntity != null)
            {
                await _patcher.Patch(existingEntity, o =>
                {
                    var annotations = o.Spec.Template.Metadata.EnsureAnnotations();
                    annotations.SetOrRemove(InjectionConstants.HashAttributeName, desiredState.Hash);
                    annotations.SetOrRemove(InjectionConstants.NameAttributeName, desiredState.Name);
                    annotations.SetOrRemove(InjectionConstants.NamespaceAttributeName, desiredState.Namespace);
                    if (!o.Spec.Template.Metadata.Annotations.Any())
                    {
                        o.Spec.Template.Metadata.Annotations = null;
                    }
                });

                await _state.MarkAsDirty(identity);
            }
        }

        private async ValueTask PatchToDesiredStateStatefulSetResource(DesiredState desiredState, NamespacedResourceIdentity identity)
        {
            var existingEntity = await _client.Get<V1StatefulSet>(identity.Name, identity.Namespace);
            if (existingEntity != null)
            {
                await _patcher.Patch(existingEntity, o =>
                {
                    var annotations = o.Spec.Template.Metadata.EnsureAnnotations();
                    annotations.SetOrRemove(InjectionConstants.HashAttributeName, desiredState.Hash);
                    annotations.SetOrRemove(InjectionConstants.NameAttributeName, desiredState.Name);
                    annotations.SetOrRemove(InjectionConstants.NamespaceAttributeName, desiredState.Namespace);
                    if (!o.Spec.Template.Metadata.Annotations.Any())
                    {
                        o.Spec.Template.Metadata.Annotations = null;
                    }
                });

                await _state.MarkAsDirty(identity);
            }
        }

        private async ValueTask PatchToDesiredStateDeployment(DesiredState desiredState, NamespacedResourceIdentity identity)
        {
            var existingEntity = await _client.Get<V1Deployment>(identity.Name, identity.Namespace);
            if (existingEntity != null)
            {
                await _patcher.Patch(existingEntity, o =>
                {
                    var annotations = o.Spec.Template.Metadata.EnsureAnnotations();
                    annotations.SetOrRemove(InjectionConstants.HashAttributeName, desiredState.Hash);
                    annotations.SetOrRemove(InjectionConstants.NameAttributeName, desiredState.Name);
                    annotations.SetOrRemove(InjectionConstants.NamespaceAttributeName, desiredState.Namespace);
                    if (!o.Spec.Template.Metadata.Annotations.Any())
                    {
                        o.Spec.Template.Metadata.Annotations = null;
                    }
                });

                await _state.MarkAsDirty(identity);
            }
        }

        private record DesiredState(string? Hash, string? Name, string? Namespace)
        {
            public static DesiredState Empty { get; } = new(null, null, null);
        }
    }
}
