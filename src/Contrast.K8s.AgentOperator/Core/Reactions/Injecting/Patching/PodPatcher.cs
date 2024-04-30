// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.Reactions.Injecting.Patching.Agents;
using Contrast.K8s.AgentOperator.Core.Telemetry.Cluster;
using Contrast.K8s.AgentOperator.Options;
using k8s.Models;
using NLog;

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

    public PodPatcher(Func<IEnumerable<IAgentPatcher>> patchersFactory, IGlobMatcher globMatcher, IClusterIdState clusterIdState, OperatorOptions operatorOptions)
    {
        _patchersFactory = patchersFactory;
        _globMatcher = globMatcher;
        _clusterIdState = clusterIdState;
        _operatorOptions = operatorOptions;
    }

    public ValueTask Patch(PatchingContext context, V1Pod pod, CancellationToken cancellationToken = default)
    {
        var patchers = _patchersFactory.Invoke();
        var patcher = patchers.FirstOrDefault(x => x.Type == context.Injector.Type);

        if (patcher is { Deprecated: true })
        {
            Logger.Warn($"Using deprecated agent injector '{patcher?.Type.ToString() ?? "Default"}'.");
        }

        if (patcher?.GetOverrideAgentMountPath() is { } agentMountPathOverride)
        {
            context = context with
            {
                AgentMountPath = agentMountPathOverride
            };
        }

        Logger.Trace($"Selected agent injector '{patcher?.Type.ToString() ?? "Default"}'.");

        ApplyPatches(context, pod, patcher);

        return ValueTask.CompletedTask;
    }

    private void ApplyPatches(PatchingContext context, V1Pod pod, IAgentPatcher? agentPatcher)
    {
        // Pod annotations.
        pod.SetAnnotation(InjectionConstants.IsInjectedAttributeName, true.ToString());
        pod.SetAnnotation(InjectionConstants.InjectedOnAttributeName, DateTimeOffset.UtcNow.ToString("O"));
        pod.SetAnnotation(InjectionConstants.InjectedByAttributeName, $"Contrast.K8s.AgentOperator/{OperatorVersion.Version}");
        pod.SetAnnotation(InjectionConstants.InjectorTypeAttributeName, context.Injector.Type.ToString());

        // Volumes.
        pod.Spec.Volumes ??= new List<V1Volume>();
        var agentVolume = new V1Volume("contrast-agent")
        {
            EmptyDir = new V1EmptyDirVolumeSource()
        };
        pod.Spec.Volumes.AddOrUpdate(agentVolume.Name, agentVolume);

        var writableVolume = new V1Volume("contrast-writable")
        {
            EmptyDir = new V1EmptyDirVolumeSource()
        };
        pod.Spec.Volumes.AddOrUpdate(writableVolume.Name, writableVolume);

        // Init Container.
        {
            var podSecurityContext = (V1PodSecurityContext?) pod.Spec.SecurityContext;
            var containerSecurityContext = pod.Spec.Containers.FirstOrDefault()?.SecurityContext;

            var initContainer = CreateInitContainer(context, agentVolume, writableVolume, podSecurityContext, containerSecurityContext);
            pod.Spec.InitContainers ??= new List<V1Container>();
            pod.Spec.InitContainers.AddOrUpdate(initContainer.Name, initContainer);
        }

        // Pull secrets.
        if (context.Injector.ImagePullSecret is { } pullSecret)
        {
            pod.Spec.ImagePullSecrets ??= new List<V1LocalObjectReference>();
            pod.Spec.ImagePullSecrets.AddOrUpdate(
                x => string.Equals(x.Name, pullSecret.Name, StringComparison.Ordinal),
                new V1LocalObjectReference(pullSecret.Name)
            );
        }

        // Normal Containers.
        foreach (var container in GetMatchingContainers(context, pod))
        {
            container.VolumeMounts ??= new List<V1VolumeMount>();

            var agentVolumeMount = new V1VolumeMount(context.AgentMountPath, agentVolume.Name, readOnlyProperty: true);
            container.VolumeMounts.AddOrUpdate(agentVolumeMount.Name, agentVolumeMount);

            var writableVolumeMount = new V1VolumeMount(context.WritableMountPath, writableVolume.Name, readOnlyProperty: false);
            container.VolumeMounts.AddOrUpdate(writableVolumeMount.Name, writableVolumeMount);

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

    private string GetVarsFromCluster(string value, V1Pod pod, PatchingContext context)
    {
        value = value.Replace("%namespace%", context.WorkloadNamespace);
        value = value.Replace("%image%", pod.Spec.Containers[0].Image);

        //labels
        string pattern = @"%labels.(.*?)%";
        var matches = System.Text.RegularExpressions.Regex.Matches(value, pattern);
        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            string labelName = match.Groups[1].Value;
            if (pod.Metadata.Labels.ContainsKey(labelName))
            {
                string labelValue = pod.Metadata.Labels[labelName];
                value = value.Replace("%labels."+labelName+"%", labelValue);
            }

        }

        //annotations
        pattern = @"%annotations.(.*?)%";
        var matchesAnnotations = System.Text.RegularExpressions.Regex.Matches(value, pattern);
        foreach (System.Text.RegularExpressions.Match match in matchesAnnotations)
        {
            string annotationName = match.Groups[1].Value;
            if(pod.Metadata.Annotations.ContainsKey(annotationName))
            {
                string annotationValue = pod.Metadata.Annotations[annotationName];
                value = value.Replace("%annotations."+annotationName+"%", annotationValue);
            }
        }
        return value;
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
        if ((containerSecurityContext?.RunAsUser ?? podSecurityContext?.RunAsUser) is {} runAsUser)
        {
            // Run as the same user as the prime container.
            securityContent.RunAsUser ??= runAsUser;
        }

        if ((containerSecurityContext?.RunAsGroup ?? podSecurityContext?.RunAsGroup) is {} runAsGroup)
        {
            // Run as the same group as the prime container.
            securityContent.RunAsGroup ??= runAsGroup;
        }

        // OpenShift default policy as of 4.10 requires explicit drops, even if "ALL" is specified.
        // So merge any drops added by policy (or user).
        securityContent.Capabilities ??= new V1Capabilities();
        securityContent.Capabilities.Drop ??= MergeDropCapabilities(containerSecurityContext);

        // https://kubernetes.io/docs/concepts/configuration/manage-resources-containers/#resource-requests-and-limits-of-pod-and-container
        const string cpuLimit = "100m";
        const string memoryLimit = "64Mi";

        var resources = new V1ResourceRequirements();

        resources.Requests ??= new Dictionary<string, ResourceQuantity>(StringComparer.Ordinal);
        resources.Requests.TryAdd("cpu", new ResourceQuantity(cpuLimit));
        resources.Requests.TryAdd("memory", new ResourceQuantity(memoryLimit));

        resources.Limits ??= new Dictionary<string, ResourceQuantity>(StringComparer.Ordinal);
        resources.Limits.TryAdd("cpu", new ResourceQuantity(cpuLimit));
        resources.Limits.TryAdd("memory", new ResourceQuantity(memoryLimit));

        var initContainer = new V1Container("contrast-init")
        {
            Image = context.Injector.Image.GetFullyQualifiedContainerImageName(),
            VolumeMounts = new List<V1VolumeMount>
            {
                new(initAgentMountPath, agentVolume.Name),
                new(initWritableMountPath, writableVolume.Name),
            },
            ImagePullPolicy = context.Injector.ImagePullPolicy,
            Env = new List<V1EnvVar>
            {
                // This isn't used in modern agent images, but is still used in older images.
                new("CONTRAST_MOUNT_PATH", initAgentMountPath),
                new("CONTRAST_MOUNT_AGENT_PATH", initAgentMountPath),
                new("CONTRAST_MOUNT_WRITABLE_PATH", initWritableMountPath),
            },
            Resources = resources,
            SecurityContext = securityContent
        };

        // This is for our CS team to aid in debugging, but also for our functional tests.
        if (securityContextTainted)
        {
            initContainer.Env.Add(new V1EnvVar("CONTRAST_DEBUGGING_SECURITY_CONTEXT_TAINTED", true.ToString()));
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
        var (workloadName, workloadNamespace, _, connection, configuration, agentMountPath, writableMountPath) = context;

        // This isn't used in modern agent images, but is still used in older images.
        yield return new V1EnvVar("CONTRAST_MOUNT_PATH", agentMountPath);
        yield return new V1EnvVar("CONTRAST_MOUNT_AGENT_PATH", agentMountPath);
        yield return new V1EnvVar("CONTRAST_MOUNT_WRITABLE_PATH", writableMountPath);

        yield return new V1EnvVar("CONTRAST__API__URL", connection.TeamServerUri);
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
                    //if value contains % then call a new function called getVarsFromCluster
                    if (value.Contains("%"))
                    {
                        yield return new V1EnvVar($"CONTRAST__{key.Replace(".", "__").ToUpperInvariant()}", GetVarsFromCluster(value, pod, context));
                    }
                    else
                    {
                        yield return new V1EnvVar($"CONTRAST__{key.Replace(".", "__").ToUpperInvariant()}", value);
                    }
                }
            }
        }

        // Order does matter here, make sure these next two are after YamlKeys.
        if (configuration?.SuppressDefaultServerName != true
            && !string.IsNullOrWhiteSpace(workloadNamespace))
        {
            yield return new V1EnvVar("CONTRAST__SERVER__NAME", $"kubernetes-{workloadNamespace}");
        }

        if (configuration?.SuppressDefaultApplicationName != true
            && !string.IsNullOrWhiteSpace(workloadName))
        {
            yield return new V1EnvVar("CONTRAST__APPLICATION__NAME", workloadName);
        }

        if (_clusterIdState.GetClusterId() is { } clusterId)
        {
            yield return new V1EnvVar("CONTRAST_CLUSTER_ID", clusterId.Guid.ToString("D"));
        }
    }
}
