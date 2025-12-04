// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.Events;
using Contrast.K8s.AgentOperator.Core.Kube;
using Contrast.K8s.AgentOperator.Core.State;
using Contrast.K8s.AgentOperator.Core.State.Resources;
using Contrast.K8s.AgentOperator.Core.State.Resources.Interfaces;
using Contrast.K8s.AgentOperator.Entities.Argo;
using Contrast.K8s.AgentOperator.Entities.OpenShift;
using JetBrains.Annotations;
using k8s.Models;
using MediatR;
using NLog;

namespace Contrast.K8s.AgentOperator.Core.Reactions.Injecting;

[UsedImplicitly]
public class PodTemplateInjectionHandler : INotificationHandler<InjectorMatched>
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly IResourceHasher _hasher;
    private readonly IStateContainer _state;
    private readonly IResourcePatcher _patcher;

    public PodTemplateInjectionHandler(IResourceHasher hasher, IStateContainer state, IResourcePatcher patcher)
    {
        _hasher = hasher;
        _state = state;
        _patcher = patcher;
    }

    public async Task Handle(InjectorMatched notification, CancellationToken cancellationToken)
    {
        if (await _state.GetIsDirty(notification.Target.Identity, cancellationToken))
        {
            return;
        }

        var (target, injector) = notification;
        var desiredState = await GetDesiredState(injector, target, cancellationToken);

        if (ChangesNeeded(target, desiredState))
        {
            Logger.Info($"Workload '{target.Identity}' will be patched (Injector: '{injector?.Identity.ToString() ?? "None"}').");
            await PatchToDesiredState(desiredState, target);
        }
    }

    private static bool ChangesNeeded(ResourceIdentityPair<IResourceWithPodTemplate> target, DesiredState desiredState)
    {
        var annotations = target.Resource.PodTemplate.Annotations;
        return (annotations.GetAnnotation(InjectionConstants.InjectorHashAttributeName) != desiredState.InjectorHash)
               || (annotations.GetAnnotation(InjectionConstants.InjectorNameAttributeName) != desiredState.InjectorName)
               || (annotations.GetAnnotation(InjectionConstants.InjectorNamespaceAttributeName) != desiredState.InjectorNamespace)
               || (annotations.GetAnnotation(InjectionConstants.WorkloadNameAttributeName) != desiredState.WorkloadName)
               || (annotations.GetAnnotation(InjectionConstants.WorkloadNamespaceAttributeName) != desiredState.WorkloadNamespace);
    }

    private async ValueTask<DesiredState> GetDesiredState(ResourceIdentityPair<AgentInjectorResource>? injector,
                                                          ResourceIdentityPair<IResourceWithPodTemplate> target,
                                                          CancellationToken cancellationToken = default)
    {
        if (injector != null
            && await _state.GetInjectorBundle(injector.Identity.Name, injector.Identity.Namespace, cancellationToken)
                is var (_, connection, configuration, secrets))
        {
            var injectorHash = _hasher.GetHash(injector.Resource, connection, configuration, secrets);
            return new DesiredState(injectorHash, injector.Identity.Name, injector.Identity.Namespace, target.Identity.Name, target.Identity.Namespace);
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
            RolloutResource => PatchToDesiredStateRollout(desiredState, identity),
            DeploymentConfigResource => PatchToDesiredStateDeploymentConfig(desiredState, identity),
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

    private async ValueTask PatchToDesiredStateRollout(DesiredState desiredState, NamespacedResourceIdentity identity)
    {
        await _state.MarkAsDirty(identity);
        await _patcher.Patch<V1Alpha1Rollout>(identity.Name, identity.Namespace, o => { PatchAnnotations(desiredState, o.Spec.Template!); });
    }

    private async ValueTask PatchToDesiredStateDeploymentConfig(DesiredState desiredState, NamespacedResourceIdentity identity)
    {
        await _state.MarkAsDirty(identity);
        await _patcher.Patch<V1DeploymentConfig>(identity.Name, identity.Namespace, o => { PatchAnnotations(desiredState, o.Spec.Template!); });
    }

    private static void PatchAnnotations(DesiredState desiredState, V1PodTemplateSpec spec)
    {
        // This call creates a new list if annotations is null.
        var annotations = spec.Metadata.EnsureAnnotations();

        // Cleanup existing if any.
        annotations.RemovePrefixed(InjectionConstants.OperatorAttributePrefix);

        // If not null, then add.
        annotations.SetOrRemove(InjectionConstants.InjectorHashAttributeName, desiredState.InjectorHash);
        annotations.SetOrRemove(InjectionConstants.InjectorNameAttributeName, desiredState.InjectorName);
        annotations.SetOrRemove(InjectionConstants.InjectorNamespaceAttributeName, desiredState.InjectorNamespace);
        annotations.SetOrRemove(InjectionConstants.WorkloadNameAttributeName, desiredState.WorkloadName);
        annotations.SetOrRemove(InjectionConstants.WorkloadNamespaceAttributeName, desiredState.WorkloadNamespace);

        // If at the end of everything, no annotations exist, delete the collection.
        // This reverses the EnsureAnnotations step.
        if (!spec.Metadata.Annotations.Any())
        {
            spec.Metadata.Annotations = null;
        }
    }

    private record DesiredState(string? InjectorHash,
                                string? InjectorName,
                                string? InjectorNamespace,
                                string? WorkloadName,
                                string? WorkloadNamespace)
    {
        public static DesiredState Empty { get; } = new(null, null, null, null, null);
    }
}
