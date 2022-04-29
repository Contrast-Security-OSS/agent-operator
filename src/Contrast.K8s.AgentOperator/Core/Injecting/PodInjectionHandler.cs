using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.Events;
using Contrast.K8s.AgentOperator.Core.State;
using Contrast.K8s.AgentOperator.Core.State.Resources;
using Contrast.K8s.AgentOperator.Core.State.Resources.Primitives;
using JetBrains.Annotations;
using k8s.Models;
using MediatR;
using NLog;

namespace Contrast.K8s.AgentOperator.Core.Injecting
{
    [UsedImplicitly]
    public class PodInjectionHandler : IRequestHandler<EntityCreating<V1Pod>, EntityCreatingMutationResult<V1Pod>>
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly IStateContainer _state;
        private readonly IGlobMatcher _globMatcher;

        public PodInjectionHandler(IStateContainer state, IGlobMatcher globMatcher)
        {
            _state = state;
            _globMatcher = globMatcher;
        }

        public async Task<EntityCreatingMutationResult<V1Pod>> Handle(EntityCreating<V1Pod> request, CancellationToken cancellationToken)
        {
            if (request.Entity.Metadata.Annotations != null
                && request.Entity.Metadata.Annotations.TryGetValue(InjectionConstants.NameAttributeName, out var injectorName)
                && request.Entity.Metadata.Annotations.TryGetValue(InjectionConstants.NamespaceAttributeName, out var injectorNamespace)
                && await _state.GetInjectorBundle(injectorName, injectorNamespace, cancellationToken)
                    is var (injector, connection, configuration))
            {
                PatchPod(request.Entity, injector, connection, configuration);

                Logger.Trace($"Patching pod using injector '{injectorNamespace}/{injectorName}'.");
                return new NeedsChangeEntityCreatingMutationResult<V1Pod>(request.Entity);
            }

            Logger.Trace("Ignored pod creation, not selected by any known agent injectors.");
            return new NoChangeEntityCreatingMutationResult<V1Pod>();
        }

        private void PatchPod(V1Pod pod, AgentInjectorResource injector, AgentConnectionResource connection, AgentConfigurationResource? configuration)
        {
            pod.SetAnnotation(InjectionConstants.IsInjectedAttributeName, true.ToString());
            pod.SetAnnotation(InjectionConstants.InjectedOnAttributeName, DateTimeOffset.UtcNow.ToString("O"));

            var volume = new V1Volume("contrast")
            {
                EmptyDir = new V1EmptyDirVolumeSource()
            };
            pod.Spec.Volumes ??= new List<V1Volume>();
            pod.Spec.Volumes.AddOrUpdate(volume.Name, volume);

            var initContainer = new V1Container("contrast-init")
            {
                Image = injector.Image.GetFullyQualifiedContainerImageName(),
                VolumeMounts = new List<V1VolumeMount>
                {
                    new("/contrast", volume.Name)
                }
            };
            pod.Spec.InitContainers ??= new List<V1Container>();
            pod.Spec.InitContainers.AddOrUpdate(initContainer.Name, initContainer);

            var matchingContainers = GetMatchingContainers(injector, pod);
            foreach (var container in matchingContainers)
            {
                var volumeMount = new V1VolumeMount("/contrast", volume.Name, readOnlyProperty: true);
                container.VolumeMounts ??= new List<V1VolumeMount>();
                container.VolumeMounts.AddOrUpdate(volumeMount.Name, volumeMount);

                foreach (var envVar in GenerateEnvVars(injector, connection, configuration, volumeMount.MountPath))
                {
                    // Don't override existing env vars.
                    if (!container.Env.Any(x => string.Equals(x.Name, envVar.Name, StringComparison.OrdinalIgnoreCase)))
                    {
                        container.Env.Add(envVar);
                    }
                }
            }
        }

        private IEnumerable<V1Container> GetMatchingContainers(AgentInjectorResource injector,
                                                               V1Pod pod)
        {
            var imagesPatterns = injector.Selector.ImagesPatterns;
            foreach (var container in pod.Spec.Containers)
            {
                if (!imagesPatterns.Any()
                    || imagesPatterns.Any(p => _globMatcher.Matches(p, container.Image)))
                {
                    yield return container;
                }
            }
        }

        private static IEnumerable<V1EnvVar> GenerateEnvVars(AgentInjectorResource injector,
                                                             AgentConnectionResource connection,
                                                             AgentConfigurationResource? configuration,
                                                             string contrastBasePath)
        {
            if (injector.Type == AgentInjectionType.DotNetCore)
            {
                // TODO Double check this for correctness.
                yield return new V1EnvVar("CORECLR_PROFILER", "{8B2CE134-0948-48CA-A4B2-80DDAD9F5791}");
                yield return new V1EnvVar("CORECLR_PROFILER_PATH", $"{contrastBasePath}/runtimes/linux-x64/native/ContrastProfiler.dll");
                yield return new V1EnvVar("CORECLR_ENABLE_PROFILING", "1");
                yield return new V1EnvVar("CONTRAST_INSTALL_SOURCE", "kubernetes-operator");
            }

            yield return new V1EnvVar("CONTRAST__API__URL", connection.TeamServerUri.ToString());
            yield return new V1EnvVar(
                "CONTRAST__API__API_KEY",
                valueFrom: new V1EnvVarSource(
                    secretKeyRef: new V1SecretKeySelector(connection.ApiKey.Key, connection.ApiKey.Name)
                )
            );
            yield return new V1EnvVar(
                "CONTRAST__API__SERVICE_KEY",
                valueFrom: new V1EnvVarSource(
                    secretKeyRef: new V1SecretKeySelector(connection.ServiceKey.Key, connection.ServiceKey.Name)
                )
            );
            yield return new V1EnvVar(
                "CONTRAST__API__USER_NAME",
                valueFrom: new V1EnvVarSource(
                    secretKeyRef: new V1SecretKeySelector(connection.UserName.Key, connection.UserName.Name)
                )
            );

            if (configuration?.YamlKeys is { } yamlKeys)
            {
                foreach (var (key, value) in yamlKeys)
                {
                    if (!string.IsNullOrWhiteSpace(key)
                        && !string.IsNullOrWhiteSpace(value))
                    {
                        yield return new V1EnvVar($"CONTRAST__{key.ToUpperInvariant()}", value);
                    }
                }
            }
        }
    }
}
