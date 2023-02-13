// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
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
            builder.Register(_ =>
            {
                var @namespace = "default";
                if (Environment.GetEnvironmentVariable("POD_NAMESPACE") is { } podNamespace)
                {
                    @namespace = podNamespace.Trim();
                }

                var settleDuration = 10;
                if (Environment.GetEnvironmentVariable("CONTRAST_SETTLE_DURATION") is { } settleDurationStr
                    && int.TryParse(settleDurationStr, out var parsedSettleDuration)
                    && parsedSettleDuration > -1)
                {
                    settleDuration = parsedSettleDuration;
                }

                var eventQueueSize = 10 * 1024;
                if (Environment.GetEnvironmentVariable("CONTRAST_EVENT_QUEUE_SIZE") is { } eventQueueSizeStr
                    && int.TryParse(eventQueueSizeStr, out var parsedEventQueueSize)
                    && parsedEventQueueSize > -1)
                {
                    eventQueueSize = parsedEventQueueSize;
                }

                // Wait:
                //   Waits for space to be available in order to complete the write operation.
                //   This might be useful for larger clusters, allow the queue to create back pressure on the "get the world" calls during startup.
                // DropOldest:
                //   Removes and ignores the oldest item in the channel in order to make room for the item being written.
                var fullMode = BoundedChannelFullMode.Wait;
                if (Environment.GetEnvironmentVariable("CONTRAST_EVENT_QUEUE_FULL_MODE") is { } fullModeStr
                    && Enum.TryParse<BoundedChannelFullMode>(fullModeStr, out var parsedFullMode))
                {
                    fullMode = parsedFullMode;
                }

                var eventQueueMergeWindowSeconds = 10;
                if (Environment.GetEnvironmentVariable("CONTRAST_EVENT_QUEUE_MERGE_WINDOW_SECONDS") is { } eventQueueMergeWindowSecondsStr
                    && int.TryParse(eventQueueMergeWindowSecondsStr, out var parsedEventQueueMergeWindowSeconds))
                {
                    eventQueueMergeWindowSeconds = parsedEventQueueMergeWindowSeconds;
                }

                // This flag will eventually default to true, and then will be removed.
                // Users may override this on a per AgentConfiguration bases via the InitContainer override field.
                var runInitContainersAsNonRoot = false;
                if (Environment.GetEnvironmentVariable("CONTRAST_RUN_INIT_CONTAINER_AS_NON_ROOT") is { } runInitContainersAsNonRootStr)
                {
                    runInitContainersAsNonRoot = runInitContainersAsNonRootStr.Equals("1", StringComparison.OrdinalIgnoreCase)
                                                 || runInitContainersAsNonRootStr.Equals("true", StringComparison.OrdinalIgnoreCase);
                }

                // This is needed for OpenShift < 4.11 (Assumed per the change log, unable to test at the time of writing).
                // See: https://github.com/openshift/cluster-kube-apiserver-operator/issues/1325
                var suppressSeccompProfile = false;
                if (Environment.GetEnvironmentVariable("CONTRAST_SUPPRESS_SECCOMP_PROFILE") is { } suppressSeccompProfileStr)
                {
                    suppressSeccompProfile = suppressSeccompProfileStr.Equals("1", StringComparison.OrdinalIgnoreCase)
                                             || suppressSeccompProfileStr.Equals("true", StringComparison.OrdinalIgnoreCase);
                }

                return new OperatorOptions(
                    @namespace,
                    settleDuration,
                    eventQueueSize,
                    fullMode,
                    eventQueueMergeWindowSeconds,
                    runInitContainersAsNonRoot,
                    suppressSeccompProfile
                );
            }).SingleInstance();

            builder.Register(_ =>
            {
                if (Environment.GetEnvironmentVariable("CONTRAST_DEFAULT_REGISTRY")
                    is { } defaultRegistry)
                {
                    return new ImageRepositoryOptions(defaultRegistry);
                }

                throw new NotImplementedException("No default registry was set.");
            }).SingleInstance();

            builder.Register(_ =>
            {
                var dnsNames = new List<string>
                {
                    "localhost"
                };

                // ingress-nginx-controller-admission,ingress-nginx-controller-admission.$(POD_NAMESPACE).svc
                if (Environment.GetEnvironmentVariable("CONTRAST_WEBHOOK_HOSTS") is { } webHookHosts)
                {
                    dnsNames.AddRange(webHookHosts.Split(",", StringSplitOptions.RemoveEmptyEntries));
                }

                return new TlsCertificateOptions("contrast-web-hook", dnsNames, TimeSpan.FromDays(365 * 100));
            }).SingleInstance();

            builder.Register(x =>
            {
                var webHookSecret = "contrast-web-hook-secret";
                if (Environment.GetEnvironmentVariable("CONTRAST_WEBHOOK_SECRET") is { } customWebHookSecret)
                {
                    webHookSecret = customWebHookSecret.Trim();
                }

                var @namespace = x.Resolve<OperatorOptions>().Namespace;

                return new TlsStorageOptions(webHookSecret, @namespace);
            }).SingleInstance();

            builder.Register(_ =>
            {
                var webHookConfigurationName = "contrast-web-hook-configuration";
                if (Environment.GetEnvironmentVariable("CONTRAST_WEBHOOK_CONFIGURATION") is { } customWebHookSecret)
                {
                    webHookConfigurationName = customWebHookSecret.Trim();
                }

                return new MutatingWebHookOptions(webHookConfigurationName);
            }).SingleInstance();

            builder.Register(x =>
            {
                var @namespace = x.Resolve<OperatorOptions>().Namespace;
                return new TelemetryOptions("contrast-cluster-id", @namespace);
            }).SingleInstance();

            builder.Register(_ =>
            {
                var enableEarlyChanging = false;
                if (Environment.GetEnvironmentVariable("CONTRAST_ENABLE_EARLY_CHAINING") is { } enableEarlyChangingStr)
                {
                    if (enableEarlyChangingStr.Equals("1", StringComparison.OrdinalIgnoreCase)
                        || enableEarlyChangingStr.Equals("true", StringComparison.OrdinalIgnoreCase))
                    {
                        enableEarlyChanging = true;
                    }
                }

                return new InjectorOptions(enableEarlyChanging);
            }).SingleInstance();
        }
    }
}
