// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Channels;
using Autofac;
using Contrast.K8s.AgentOperator.Options;
using JetBrains.Annotations;

namespace Contrast.K8s.AgentOperator.Modules
{
    [UsedImplicitly]
    public class OptionsModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.Register(context =>
            {
                var logger = context.Resolve<IOptionsLogger>();

                var @namespace = "contrast-agent-operator";
                if (GetEnvironmentVariableAsString("POD_NAMESPACE", out var namespaceStr))
                {
                    logger.LogOptionValue("pod-namespace", @namespace, namespaceStr);
                    @namespace = namespaceStr;
                }

                var settleDuration = 10;
                if (GetEnvironmentVariableAsInt("CONTRAST_SETTLE_DURATION", out var parsedSettleDuration)
                    && parsedSettleDuration > -1)
                {
                    logger.LogOptionValue("settle-duration", settleDuration, parsedSettleDuration);
                    settleDuration = parsedSettleDuration;
                }

                var eventQueueSize = 10 * 1024;
                if (GetEnvironmentVariableAsInt("CONTRAST_EVENT_QUEUE_SIZE", out var parsedEventQueueSize)
                    && parsedEventQueueSize > -1)
                {
                    logger.LogOptionValue("event-queue-size", eventQueueSize, parsedEventQueueSize);
                    eventQueueSize = parsedEventQueueSize;
                }

                // Wait:
                //   Waits for space to be available in order to complete the write operation.
                //   This might be useful for larger clusters, allow the queue to create back pressure on the "get the world" calls during startup.
                // DropOldest:
                //   Removes and ignores the oldest item in the channel in order to make room for the item being written.
                var fullMode = BoundedChannelFullMode.Wait;
                if (GetEnvironmentVariableAsString("CONTRAST_EVENT_QUEUE_FULL_MODE", out var fullModeStr)
                    && Enum.TryParse<BoundedChannelFullMode>(fullModeStr, out var parsedFullMode))
                {
                    logger.LogOptionValue("event-queue-full-mode", fullMode.ToString(), parsedFullMode.ToString());
                    fullMode = parsedFullMode;
                }

                var eventQueueMergeWindowSeconds = 10;
                if (GetEnvironmentVariableAsInt("CONTRAST_EVENT_QUEUE_MERGE_WINDOW_SECONDS", out var parsedEventQueueMergeWindowSeconds))
                {
                    logger.LogOptionValue("event-queue-merge-window", eventQueueMergeWindowSeconds, parsedEventQueueMergeWindowSeconds);
                    eventQueueMergeWindowSeconds = parsedEventQueueMergeWindowSeconds;
                }

                // Users may override this on a per AgentConfiguration bases via the InitContainer override field.
                var runInitContainersAsNonRoot = true;
                if (GetEnvironmentVariableAsBoolean("CONTRAST_RUN_INIT_CONTAINER_AS_NON_ROOT", out var parsedRunInitContainersAsNonRoot))
                {
                    logger.LogOptionValue("run-init-container-as-non-root", runInitContainersAsNonRoot, parsedRunInitContainersAsNonRoot);
                    runInitContainersAsNonRoot = parsedRunInitContainersAsNonRoot;
                }

                // This is needed for OpenShift < 4.11 (Assumed per the change log, unable to test at the time of writing).
                // See: https://github.com/openshift/cluster-kube-apiserver-operator/issues/1325
                var suppressSeccompProfile = false;
                if (GetEnvironmentVariableAsBoolean("CONTRAST_SUPPRESS_SECCOMP_PROFILE", out var parsedSeccompProfile))
                {
                    logger.LogOptionValue("suppress-seccomp-profile", suppressSeccompProfile, parsedSeccompProfile);
                    suppressSeccompProfile = parsedSeccompProfile;
                }

                // A value from 0-100 to denote how many options the operator should purposely fail in.
                // The goal is to test and correctly handle a non-perfect cluster.
                var chaosPercent = 0;
                if (GetEnvironmentVariableAsInt("CONTRAST_CHAOS_RATIO", out var parsedChaosPercent)
                    && parsedChaosPercent > 0)
                {
                    logger.LogOptionValue("chaos-percent", chaosPercent, parsedChaosPercent);
                    chaosPercent = parsedChaosPercent;
                }

                return new OperatorOptions(
                    @namespace,
                    settleDuration,
                    eventQueueSize,
                    fullMode,
                    eventQueueMergeWindowSeconds,
                    runInitContainersAsNonRoot,
                    suppressSeccompProfile,
                    chaosPercent / 100m
                );
            }).SingleInstance();

            builder.Register(context =>
            {
                var logger = context.Resolve<IOptionsLogger>();

                var defaultRegistry = "contrast";
                if (GetEnvironmentVariableAsString("CONTRAST_DEFAULT_REGISTRY", out var parsedDefaultRegistry))
                {
                    logger.LogOptionValue("default-registry", defaultRegistry, parsedDefaultRegistry);
                    defaultRegistry = parsedDefaultRegistry;
                }

                return new ImageRepositoryOptions(defaultRegistry);
            }).SingleInstance();

            builder.Register(context =>
            {
                var logger = context.Resolve<IOptionsLogger>();

                IReadOnlyCollection<string> dnsNames = new HashSet<string>(StringComparer.Ordinal)
                {
                    "localhost",
                    "contrast-agent-operator",
                    "contrast-agent-operator.contrast-agent-operator.svc",
                    "contrast-agent-operator.contrast-agent-operator.svc.cluster.local"
                };

                // ingress-nginx-controller-admission,ingress-nginx-controller-admission.$(POD_NAMESPACE).svc
                if (GetEnvironmentVariableAsString("CONTRAST_WEBHOOK_HOSTS", out var webHookHosts))
                {
                    var customHosts = new HashSet<string>(StringComparer.Ordinal);

                    var parsedHosts = webHookHosts.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var parsedHost in parsedHosts)
                    {
                        var normalizedHost = parsedHost.ToLowerInvariant().Trim();
                        customHosts.Add(normalizedHost);
                    }

                    customHosts.Add("localhost");

                    // Sort since this is a HashSet...
                    logger.LogOptionValue("webhook-hosts", string.Join(", ", dnsNames.OrderBy(x => x)), string.Join(", ", customHosts.OrderBy(x => x)));
                    dnsNames = customHosts;
                }

                return new TlsCertificateOptions("contrast-web-hook", dnsNames, TimeSpan.FromDays(365 * 100));
            }).SingleInstance();

            builder.Register(context =>
            {
                var logger = context.Resolve<IOptionsLogger>();
                var @namespace = context.Resolve<OperatorOptions>().Namespace;

                var webHookSecret = "contrast-web-hook-secret";
                if (GetEnvironmentVariableAsString("CONTRAST_WEBHOOK_SECRET", out var customWebHookSecret))
                {
                    logger.LogOptionValue("webhook-secret-name", webHookSecret, customWebHookSecret);
                    webHookSecret = customWebHookSecret;
                }

                return new TlsStorageOptions(webHookSecret, @namespace);
            }).SingleInstance();

            builder.Register(context =>
            {
                var logger = context.Resolve<IOptionsLogger>();

                var webHookConfigurationName = "contrast-web-hook-configuration";
                if (GetEnvironmentVariableAsString("CONTRAST_WEBHOOK_CONFIGURATION", out var customWebhookConfiguration))
                {
                    logger.LogOptionValue("webhook-configuration-name", webHookConfigurationName, customWebhookConfiguration);
                    webHookConfigurationName = customWebhookConfiguration;
                }

                return new MutatingWebHookOptions(webHookConfigurationName);
            }).SingleInstance();

            builder.Register(context =>
            {
                var logger = context.Resolve<IOptionsLogger>();
                var @namespace = context.Resolve<OperatorOptions>().Namespace;

                var installSource = "unknown";
                if (GetEnvironmentVariableAsString("CONTRAST_INSTALL_SOURCE", out var installSourceStr))
                {
                    logger.LogOptionValue("install-source", installSource, installSourceStr);
                    installSource = installSourceStr;
                }

                return new TelemetryOptions("contrast-cluster-id", @namespace, installSource);
            }).SingleInstance();

            builder.Register(context =>
            {
                var logger = context.Resolve<IOptionsLogger>();

                var enableEarlyChaining = false;
                if (GetEnvironmentVariableAsBoolean("CONTRAST_ENABLE_EARLY_CHAINING", out var parsedChainingResult))
                {
                    logger.LogOptionValue("enable-early-chaining", enableEarlyChaining, parsedChainingResult);
                    enableEarlyChaining = parsedChainingResult;
                }

                var enablePythonRewrite = true;
                if (GetEnvironmentVariableAsBoolean("CONTRAST_ENABLE_PYTHON_REWRITE", out var parsedRewriteResult))
                {
                    logger.LogOptionValue("enable-python-rewrite", enablePythonRewrite, parsedRewriteResult);
                    enablePythonRewrite = parsedRewriteResult;
                }

                return new InjectorOptions(enableEarlyChaining, enablePythonRewrite);
            }).SingleInstance();
        }

        private static bool GetEnvironmentVariableAsString(string variable, [NotNullWhen(true)] out string? parsedResult)
        {
            if (Environment.GetEnvironmentVariable(variable) is { } valueStr)
            {
                var normalizedValueStr = valueStr.Trim();
                parsedResult = normalizedValueStr;
                return true;
            }

            parsedResult = default;
            return false;
        }

        private static bool GetEnvironmentVariableAsInt(string variable, out int parsedResult)
        {
            if (Environment.GetEnvironmentVariable(variable) is { } valueStr)
            {
                // Just to match how we handle values in agents.
                var normalizedValueStr = valueStr.Trim()
                                                 .Replace("_", "")
                                                 .Replace(",", "");

                if (int.TryParse(normalizedValueStr, out parsedResult))
                {
                    return true;
                }
            }

            parsedResult = default;
            return false;
        }

        private static bool GetEnvironmentVariableAsBoolean(string variable, out bool parsedResult)
        {
            if (Environment.GetEnvironmentVariable(variable) is { } valueStr)
            {
                var normalizedValueStr = valueStr.Trim();
                parsedResult = normalizedValueStr.Equals("1", StringComparison.OrdinalIgnoreCase)
                               || normalizedValueStr.Equals("true", StringComparison.OrdinalIgnoreCase);

                return true;
            }

            parsedResult = default;
            return false;
        }
    }
}
