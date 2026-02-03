// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Contrast.K8s.AgentOperator.Core.Reactions.Injecting.Patching.Agents;
using Contrast.K8s.AgentOperator.Core.Reactions.Matching;
using Contrast.K8s.AgentOperator.Core.Telemetry.Cluster;
using Contrast.K8s.AgentOperator.Options;
using k8s.Models;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Contrast.K8s.AgentOperator.Core.Reactions.Injecting.Patching;

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
    private readonly OperatorOptions _operatorOptions;
    private readonly InitContainerOptions _initOptions;
    private readonly TelemetryOptions _telemetryOptions;

    public PodPatcher(Func<IEnumerable<IAgentPatcher>> patchersFactory, IGlobMatcher globMatcher,
            IClusterIdState clusterIdState, OperatorOptions operatorOptions, InitContainerOptions initOptions,
            TelemetryOptions telemetryOptions)
    {
        _patchersFactory = patchersFactory;
        _globMatcher = globMatcher;
        _clusterIdState = clusterIdState;
        _operatorOptions = operatorOptions;
        _initOptions = initOptions;
        _telemetryOptions = telemetryOptions;
    }

    public ValueTask Patch(PatchingContext context, V1Pod pod, CancellationToken cancellationToken = default)
    {
        var patchers = _patchersFactory.Invoke();
        var patcher = patchers.FirstOrDefault(x => x.Type == context.Injector.Type);

        if (patcher is { Deprecated: true })
        {
            Logger.Warn($"Using deprecated agent injector type '{AgentInjectionTypeConverter.GetStringFromType(patcher.Type)}'. {patcher.DeprecatedMessage}");
        }

        if (patcher?.GetOverrideAgentMountPath() is { } agentMountPathOverride)
        {
            context = context with
            {
                AgentMountPath = agentMountPathOverride
            };
        }

        Logger.Trace($"Selected agent injector '{AgentInjectionTypeConverter.GetStringFromType(patcher?.Type)}'.");

        ApplyPatches(context, pod, patcher);

        return ValueTask.CompletedTask;
    }

    private void ApplyPatches(PatchingContext context, V1Pod pod, IAgentPatcher? agentPatcher)
    {
        // Pod annotations.
        pod.SetAnnotation(InjectionConstants.IsInjectedAttributeName, true.ToString());
        pod.SetAnnotation(InjectionConstants.InjectedOnAttributeName, DateTimeOffset.UtcNow.ToString("O"));
        pod.SetAnnotation(InjectionConstants.InjectedByAttributeName,
            $"Contrast.K8s.AgentOperator/{OperatorVersion.Version}");
        pod.SetAnnotation(InjectionConstants.InjectorTypeAttributeName, context.Injector.Type.ToString());

        // Volumes.
        pod.Spec.Volumes ??= new List<V1Volume>();
        var agentVolume = new V1Volume
        {
            Name = "contrast-agent",
            EmptyDir = new V1EmptyDirVolumeSource()
        };
        pod.Spec.Volumes.AddOrUpdate(agentVolume.Name, agentVolume);

        var writableVolume = new V1Volume
        {
            Name = "contrast-writable",
            EmptyDir = new V1EmptyDirVolumeSource()
        };
        pod.Spec.Volumes.AddOrUpdate(writableVolume.Name, writableVolume);
        var connectionSecretVolumeName = "contrast-connection";
        var connectionVolumeRef = context.ConnectionVolumeSecret;
        if (connectionVolumeRef != null)
        {
            var secretVolume = new V1Volume
            {
                Name = connectionSecretVolumeName,
                Secret = new V1SecretVolumeSource
                {
                    SecretName = connectionVolumeRef.Name,
                    Optional = true
                }
            };
            pod.Spec.Volumes.AddOrUpdate(secretVolume.Name, secretVolume);
        }

        // Init Container.
        {
            var podSecurityContext = (V1PodSecurityContext?)pod.Spec.SecurityContext;
            var containerSecurityContext = pod.Spec.Containers.FirstOrDefault()?.SecurityContext;

            var initContainer = CreateInitContainer(context, agentVolume, writableVolume, podSecurityContext,
                containerSecurityContext);
            pod.Spec.InitContainers ??= new List<V1Container>();
            pod.Spec.InitContainers.AddOrUpdate(initContainer.Name, initContainer);
        }

        // Pull secrets.
        if (context.Injector.ImagePullSecret is { } pullSecret)
        {
            pod.Spec.ImagePullSecrets ??= new List<V1LocalObjectReference>();
            pod.Spec.ImagePullSecrets.AddOrUpdate(
                x => string.Equals(x.Name, pullSecret.Name, StringComparison.Ordinal),
                new V1LocalObjectReference{ Name = pullSecret.Name }
            );
        }

        // Normal Containers.
        foreach (var container in GetMatchingContainers(context, pod))
        {
            container.VolumeMounts ??= new List<V1VolumeMount>();

            var agentVolumeMount = new V1VolumeMount
            {
                Name = agentVolume.Name,
                MountPath = context.AgentMountPath,
                ReadOnlyProperty = true
            };
            container.VolumeMounts.AddOrUpdate(agentVolumeMount.Name, agentVolumeMount);

            var writableVolumeMount = new V1VolumeMount
            {
                Name = writableVolume.Name,
                MountPath = context.WritableMountPath,
                ReadOnlyProperty = false
            };
            container.VolumeMounts.AddOrUpdate(writableVolumeMount.Name, writableVolumeMount);

            if (connectionVolumeRef != null)
            {
                var connectionSecretVolumeMount = new V1VolumeMount
                {
                    Name = connectionSecretVolumeName,
                    MountPath = connectionVolumeRef.MountPath,
                    ReadOnlyProperty = true
                };
                container.VolumeMounts.AddOrUpdate(connectionSecretVolumeMount.Name, connectionSecretVolumeMount);
            }

            var genericPatches = GenerateEnvVars(context, pod);
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

    private V1Container CreateInitContainer(PatchingContext context,
        V1Volume agentVolume,
        V1Volume writableVolume,
        V1PodSecurityContext? podSecurityContext,
        V1SecurityContext? containerSecurityContext)
    {
        const string initAgentMountPath = "/contrast-init/agent";
        const string initWritableMountPath = "/contrast-init/data";

        var securityContextTainted = context.Configuration?.InitContainerOverrides?.SecurityContext != null;
        var securityContent = context.Configuration?.InitContainerOverrides?.SecurityContext
                              ?? new V1SecurityContext();

        // https://kubernetes.io/docs/concepts/security/pod-security-standards/
        // We cannot safely enable this as this will break existing operator deployments or injections with older agents.
        // In the future we will default to this being enabled, but for now (at least during the beta) this needs to default to false.
        // (we need the container image to denote the user, not us, or we might break OpenShift)
        // It appears OpenShift is okay with this being true, if the container image sets a user. This differs from upstream K8s.
        securityContent.RunAsNonRoot ??= _operatorOptions.RunInitContainersAsNonRoot;
        securityContent.AllowPrivilegeEscalation ??= false;
        securityContent.Privileged ??= false;
        securityContent.ReadOnlyRootFilesystem ??= true;

        // OpenShift's default restrictive policy disallows setting the SeccompProfile.Type to anything other than null.
        // This differs from upstream K8s and friends.
        // See: https://github.com/openshift/cluster-kube-apiserver-operator/issues/1325
        // This is needed for K8s's restrictive policy 1.23+.
        if (!_operatorOptions.SuppressSeccompProfile)
        {
            securityContent.SeccompProfile ??= new V1SeccompProfile();
            securityContent.SeccompProfile.Type ??= "RuntimeDefault";
        }

        // In OpenShift, there's a race condition around operator mutating webhooks and the build-in mutating webhook that applies security policies.
        // If a mutating webhook adds a sidecar/init container, security policies are not re-applied.
        // This is continuously brought up as an issue since at least 2019... since reinvocationPolicy was added to upstream.
        if ((containerSecurityContext?.RunAsUser ?? podSecurityContext?.RunAsUser) is { } runAsUser)
        {
            // Run as the same user as the prime container.
            securityContent.RunAsUser ??= runAsUser;
        }

        if ((containerSecurityContext?.RunAsGroup ?? podSecurityContext?.RunAsGroup) is { } runAsGroup)
        {
            // Run as the same group as the prime container.
            securityContent.RunAsGroup ??= runAsGroup;
        }

        // OpenShift default policy as of 4.10 requires explicit drops, even if "ALL" is specified.
        // So merge any drops added by policy (or user).
        securityContent.Capabilities ??= new V1Capabilities();
        securityContent.Capabilities.Drop ??= MergeDropCapabilities(containerSecurityContext);

        // https://kubernetes.io/docs/concepts/configuration/manage-resources-containers/#resource-requests-and-limits-of-pod-and-container
        var resources = new V1ResourceRequirements();

        resources.Requests ??= new Dictionary<string, ResourceQuantity>(StringComparer.Ordinal);
        resources.Requests.TryAdd("cpu", new ResourceQuantity(_initOptions.CpuRequest));
        resources.Requests.TryAdd("memory", new ResourceQuantity(_initOptions.MemoryRequest));
        resources.Requests.TryAdd("ephemeral-storage", new ResourceQuantity(_initOptions.EphemeralStorageRequest));

        resources.Limits ??= new Dictionary<string, ResourceQuantity>(StringComparer.Ordinal);
        resources.Limits.TryAdd("cpu", new ResourceQuantity(_initOptions.CpuLimit));
        resources.Limits.TryAdd("memory", new ResourceQuantity(_initOptions.MemoryLimit));
        resources.Limits.TryAdd("ephemeral-storage", new ResourceQuantity(_initOptions.EphemeralStorageLimit));

        var initContainer = new V1Container
        {
            Name = "contrast-init",
            Image = context.Injector.Image.GetFullyQualifiedContainerImageName(),
            VolumeMounts = new List<V1VolumeMount>
            {
                new() { MountPath = initAgentMountPath, Name = agentVolume.Name },
                new() { MountPath = initWritableMountPath, Name = writableVolume.Name },
            },
            ImagePullPolicy = context.Injector.ImagePullPolicy,
            Env = new List<V1EnvVar>
            {
                new() { Name = "CONTRAST_MOUNT_PATH", Value = initAgentMountPath },
                new() { Name = "CONTRAST_MOUNT_AGENT_PATH", Value = initAgentMountPath },
                new() { Name = "CONTRAST_MOUNT_WRITABLE_PATH", Value = initWritableMountPath },
            },
            Resources = resources,
            SecurityContext = securityContent
        };

        // This is for our CS team to aid in debugging, but also for our functional tests.
        if (securityContextTainted)
        {
            initContainer.Env.Add(new V1EnvVar { Name = "CONTRAST_DEBUGGING_SECURITY_CONTEXT_TAINTED", Value = "true" });
        }

        return initContainer;
    }

    private static IList<string> MergeDropCapabilities(V1SecurityContext? containerSecurityContext)
    {
        const string defaultDrop = "ALL"; // K8s docs sometimes say 'all' is valid, but the security policy in v1.25 requires 'ALL'.

        if (containerSecurityContext?.Capabilities?.Drop is { Count: > 0 } existingCapabilitiesDrop)
        {
            return new HashSet<string>(existingCapabilitiesDrop, StringComparer.OrdinalIgnoreCase)
            {
                defaultDrop
            }.ToList();
        }

        return new List<string>
        {
            defaultDrop
        };
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

    private IEnumerable<V1EnvVar> GenerateEnvVars(PatchingContext context, V1Pod pod)
    {
        var (workloadName, workloadNamespace, _, connection, configuration, connectionVolumeRef, agentMountPath, writableMountPath) = context;

        // This isn't used in modern agent images, but is still used in older images.
        yield return new V1EnvVar { Name = "CONTRAST_MOUNT_PATH", Value = agentMountPath };
        yield return new V1EnvVar { Name = "CONTRAST_MOUNT_AGENT_PATH", Value = agentMountPath };
        yield return new V1EnvVar { Name = "CONTRAST_MOUNT_WRITABLE_PATH", Value = writableMountPath };

        //If opt-out is set on the operator we should opt-out the agents
        if (_telemetryOptions.OptOut)
        {
            yield return new V1EnvVar { Name = "CONTRAST_AGENT_TELEMETRY_OPTOUT", Value = "1" };
        }

        if (connection.TeamServerUri != null)
        {
            yield return new V1EnvVar { Name = "CONTRAST__API__URL", Value = connection.TeamServerUri };
        }

        if (connectionVolumeRef != null)
        {
            yield return new V1EnvVar { Name = "CONTRAST_CONFIG_PATH", Value = Path.Join(connectionVolumeRef.MountPath, connectionVolumeRef.Key) };
        }
        else
        {
            // New auth method
            if (connection.Token != null)
            {
                yield return new V1EnvVar
                {
                    Name = "CONTRAST__API__TOKEN",
                    ValueFrom = new V1EnvVarSource
                    {
                        SecretKeyRef = new V1SecretKeySelector
                        {
                            Key = connection.Token.Key,
                            Name = connection.Token.Name
                        }
                    }
                };
            }

            // Legacy auth method
            if (connection.ApiKey != null)
            {
                yield return new V1EnvVar
                {
                    Name = "CONTRAST__API__API_KEY",
                    ValueFrom = new V1EnvVarSource
                    {
                        SecretKeyRef = new V1SecretKeySelector
                        {
                            Key = connection.ApiKey.Key,
                            Name = connection.ApiKey.Name
                        }
                    }
                };
            }

            if (connection.ServiceKey != null)
            {
                yield return new V1EnvVar
                {
                    Name = "CONTRAST__API__SERVICE_KEY",
                    ValueFrom = new V1EnvVarSource
                    {
                        SecretKeyRef = new V1SecretKeySelector
                        {
                            Key = connection.ServiceKey.Key,
                            Name = connection.ServiceKey.Name
                        }
                    }
                };
            }

            if (connection.UserName != null)
            {
                yield return new V1EnvVar
                {
                    Name = "CONTRAST__API__USER_NAME",
                    ValueFrom = new V1EnvVarSource
                    {
                        SecretKeyRef = new V1SecretKeySelector
                        {
                            Key = connection.UserName.Key,
                            Name = connection.UserName.Name
                        }
                    }
                };
            }
        }


        if (configuration?.YamlKeys is { } yamlKeys)
        {
            foreach (var (key, value) in yamlKeys)
            {
                if (!string.IsNullOrWhiteSpace(key)
                    && !string.IsNullOrWhiteSpace(value))
                {
                    if (configuration?.EnableYamlVariableReplacement == true && value.Contains('%'))
                    {
                        var replacement = GetVariableReplacements(value, pod);
                        if (replacement != null)
                        {
                            foreach (var envVar in replacement.AdditionalEnvVars)
                            {
                                yield return envVar;
                            }

                            yield return new V1EnvVar { Name = $"CONTRAST__{key.Replace(".", "__").ToUpperInvariant()}", Value = replacement.Value };
                        }
                    }
                    else
                    {
                        yield return new V1EnvVar { Name = $"CONTRAST__{key.Replace(".", "__").ToUpperInvariant()}", Value = value };
                    }
                }
            }
        }

        // Order does matter here, YamlKeys values take precedent.
        if (_operatorOptions.EnableAgentStdout)
        {
            yield return new V1EnvVar { Name = "CONTRAST__AGENT__LOGGER__STDOUT", Value = "true" };
        }

        if (configuration?.SuppressDefaultServerName != true
            && !string.IsNullOrWhiteSpace(workloadNamespace))
        {
            yield return new V1EnvVar { Name = "CONTRAST__SERVER__NAME", Value = $"kubernetes-{workloadNamespace}" };
        }

        if (configuration?.SuppressDefaultApplicationName != true
            && !string.IsNullOrWhiteSpace(workloadName))
        {
            yield return new V1EnvVar { Name = "CONTRAST__APPLICATION__NAME", Value = workloadName };
        }

        if (_clusterIdState.GetClusterId() is { } clusterId)
        {
            yield return new V1EnvVar { Name = "CONTRAST_CLUSTER_ID", Value = clusterId.Guid.ToString("D") };
        }
    }

    private VariableReplacement? GetVariableReplacements(string value, V1Pod pod)
    {
        try
        {
            // Attempt to use the downwardApi for this data because it has the most up-to-date information
            // because it is populated by k8s at pod startup, otherwise use the information the operator has
            // though it may be out-of-date since the operator doesn't have a perfect picture of the cluster
            // https://kubernetes.io/docs/concepts/workloads/pods/downward-api/

            var additionalVars = new List<V1EnvVar>();

            //Pattern matching for everything starting with % and ending with %
            const string pattern = "%(.*?)%";
            var matches = Regex.Matches(value, pattern);
            foreach (Match match in matches)
            {
                var key = match.Groups[1].Value;
                if (key.Equals("namespace", StringComparison.OrdinalIgnoreCase))
                {
                    additionalVars.Add(new V1EnvVar
                    {
                        Name = "CONTRAST_VAR_POD_NAMESPACE",
                        ValueFrom = new V1EnvVarSource
                        {
                            FieldRef = new V1ObjectFieldSelector { FieldPath = "metadata.namespace" }
                        }
                    });

                    value = value.Replace("%namespace%", "$(CONTRAST_VAR_POD_NAMESPACE)");
                }
                else if (key.StartsWith("labels", StringComparison.OrdinalIgnoreCase))
                {
                    if (key.Length <= "labels.".Length)
                    {
                        Logger.Warn($"Invalid 'labels' variable in yaml: '{key}'");
                        continue;
                    }

                    var labelKey = key.Substring("labels.".Length);
                    var envKey = labelKey.Replace("/", "").Replace("-", "").Replace(".", "").ToUpper();
                    var envVariableName = $"CONTRAST_VAR_LABEL_{envKey}";

                    additionalVars.Add(new V1EnvVar
                    {
                        Name = envVariableName,
                        ValueFrom = new V1EnvVarSource
                        {
                            FieldRef = new V1ObjectFieldSelector { FieldPath = $"metadata.labels['{labelKey}']" }
                        }
                    });

                    value = value.Replace($"%{key}%", $"$({envVariableName})");
                }
                else if (key.StartsWith("annotations", StringComparison.OrdinalIgnoreCase))
                {
                    if (key.Length <= "annotations.".Length)
                    {
                        Logger.Warn($"Invalid 'annotations' variable in yaml: '{key}'");
                        continue;
                    }

                    var annotationKey = key.Substring("annotations.".Length);
                    var envKey = annotationKey.Replace("/", "").Replace("-", "").Replace(".", "").ToUpper();
                    var envVariableName = $"CONTRAST_VAR_ANNOTATION_{envKey}";

                    additionalVars.Add(new V1EnvVar
                    {
                        Name = envVariableName,
                        ValueFrom = new V1EnvVarSource
                        {
                            FieldRef = new V1ObjectFieldSelector { FieldPath = $"metadata.annotations['{annotationKey}']" }
                        }
                    });

                    value = value.Replace($"%{key}%", $"$({envVariableName})");
                }
                else if (key.StartsWith("container", StringComparison.OrdinalIgnoreCase))
                {
                    var containerVar = ReplaceContainerVariable(key, value, pod);
                    if (containerVar != null)
                    {
                        value = containerVar;
                    }
                }
                else
                {
                    Logger.Warn($"Unknown variable in yaml: '{key}'");
                }
            }

            return new VariableReplacement(value, additionalVars);
        }
        catch (Exception e)
        {
            Logger.Error($"Failed to parse '{value}' for variable replacement: {e}");
            return null;
        }
    }

    private string? ReplaceContainerVariable(string key, string value, V1Pod pod)
    {
        var containerParts = key.Split('.');
        if (containerParts.Length != 3)
        {
            Logger.Warn($"Invalid 'container' variable in yaml: '{key}'");
            return null;
        }

        var containerName = containerParts[1];
        var containerKey = containerParts[2];
        var container = pod.Spec.Containers.SingleOrDefault(x =>
            x.Name.Equals(containerName, StringComparison.OrdinalIgnoreCase));
        if (container == null)
        {
            Logger.Warn($"Container name '{containerName}' not found for yaml variable: '{key}'");
            return null;
        }

        if (containerKey.Equals("image", StringComparison.OrdinalIgnoreCase))
        {
            return value.Replace($"%{key}%", container.Image);
        }

        Logger.Warn($"Unknown 'container' variable in yaml: '{key}'");
        return null;
    }
}
