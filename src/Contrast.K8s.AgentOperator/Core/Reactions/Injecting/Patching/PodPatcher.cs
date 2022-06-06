using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.Reactions.Injecting.Patching.Agents;
using Contrast.K8s.AgentOperator.Core.Telemetry.Cluster;
using k8s.Models;
using NLog;

namespace Contrast.K8s.AgentOperator.Core.Reactions.Injecting.Patching
{
    public interface IPodPatcher
    {
        ValueTask Patch(PatchingContext context, V1Pod pod, CancellationToken cancellationToken = default);
    }

    public class PodPatcher : IPodPatcher
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly Func<IEnumerable<IAgentPatcher>> _patchersFactory;
        private readonly IGlobMatcher _globMatcher;
        private readonly IClusterIdState _clusterIdState;

        public PodPatcher(Func<IEnumerable<IAgentPatcher>> patchersFactory, IGlobMatcher globMatcher, IClusterIdState clusterIdState)
        {
            _patchersFactory = patchersFactory;
            _globMatcher = globMatcher;
            _clusterIdState = clusterIdState;
        }

        public ValueTask Patch(PatchingContext context, V1Pod pod, CancellationToken cancellationToken = default)
        {
            var patchers = _patchersFactory.Invoke();
            var patcher = patchers.FirstOrDefault(x => x.Type == context.Injector.Type);

            if (patcher?.GetMountPath() is { } mountPathOverride)
            {
                context = context with
                {
                    ContrastMountPath = mountPathOverride
                };
            }

            Logger.Trace($"Selected agent injector '{patcher?.Type}'.");

            ApplyPatches(context, pod, patcher);

            return ValueTask.CompletedTask;
        }

        private void ApplyPatches(PatchingContext context, V1Pod pod, IAgentPatcher? agentPatcher)
        {
            pod.SetAnnotation(InjectionConstants.IsInjectedAttributeName, true.ToString());
            pod.SetAnnotation(InjectionConstants.InjectedOnAttributeName, DateTimeOffset.UtcNow.ToString("O"));

            var volume = new V1Volume("contrast")
            {
                EmptyDir = new V1EmptyDirVolumeSource()
            };
            pod.Spec.Volumes ??= new List<V1Volume>();
            pod.Spec.Volumes.AddOrUpdate(volume.Name, volume);

            const string initTargetMountPath = "/contrast-init";
            var initContainer = new V1Container("contrast-init")
            {
                Image = context.Injector.Image.GetFullyQualifiedContainerImageName(),
                VolumeMounts = new List<V1VolumeMount>
                {
                    new(initTargetMountPath, volume.Name)
                },
                ImagePullPolicy = context.Injector.ImagePullPolicy,
                Env = new List<V1EnvVar>
                {
                    new("CONTRAST_MOUNT_PATH", initTargetMountPath)
                }
            };
            pod.Spec.InitContainers ??= new List<V1Container>();
            pod.Spec.InitContainers.AddOrUpdate(initContainer.Name, initContainer);

            if (context.Injector.ImagePullSecret is { } pullSecret)
            {
                pod.Spec.ImagePullSecrets ??= new List<V1LocalObjectReference>();
                pod.Spec.ImagePullSecrets.AddOrUpdate(x => string.Equals(x.Name, pullSecret.Name, StringComparison.Ordinal),
                    new V1LocalObjectReference(pullSecret.Name));
            }

            var matchingContainers = GetMatchingContainers(context, pod);
            foreach (var container in matchingContainers)
            {
                var volumeMount = new V1VolumeMount(context.ContrastMountPath, volume.Name, readOnlyProperty: true);
                container.VolumeMounts ??= new List<V1VolumeMount>();
                container.VolumeMounts.AddOrUpdate(volumeMount.Name, volumeMount);

                var genericPatches = GenerateEnvVars(context);
                var agentPatches = agentPatcher?.GenerateEnvVars(context) ?? Array.Empty<V1EnvVar>();

                foreach (var envVar in genericPatches.Concat(agentPatches))
                {
                    container.Env ??= new List<V1EnvVar>();

                    // Don't override existing env vars.
                    if (!container.Env.Any(x => string.Equals(x.Name, envVar.Name, StringComparison.OrdinalIgnoreCase)))
                    {
                        container.Env.Add(envVar);
                    }
                }

                agentPatcher?.PatchContainer(container, context);
            }
        }

        private IEnumerable<V1Container> GetMatchingContainers(PatchingContext context, V1Pod pod)
        {
            var imagesPatterns = context.Injector.Selector.ImagesPatterns;
            foreach (var container in pod.Spec.Containers)
            {
                if (!imagesPatterns.Any()
                    || imagesPatterns.Any(p => _globMatcher.Matches(p, container.Image)))
                {
                    yield return container;
                }
            }
        }

        private IEnumerable<V1EnvVar> GenerateEnvVars(PatchingContext context)
        {
            var (_, connection, configuration, contrastMountPath) = context;

            yield return new V1EnvVar("CONTRAST_MOUNT_PATH", contrastMountPath);

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
                        yield return new V1EnvVar($"CONTRAST__{key.Replace(".", "__").ToUpperInvariant()}", value);
                    }
                }
            }

            if (_clusterIdState.GetClusterId() is { } clusterId)
            {
                yield return new V1EnvVar("CONTRAST_CLUSTER_ID", clusterId.Guid.ToString("D"));
            }
        }
    }
}
