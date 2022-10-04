// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.Kube;
using Contrast.K8s.AgentOperator.Options;
using DotnetKubernetesClient;
using k8s.Models;
using NLog;

namespace Contrast.K8s.AgentOperator.Core.Tls
{
    public interface IKubeWebHookConfigurationWriter
    {
        ValueTask<V1Secret?> FetchCurrentCertificate();
        ValueTask UpdateClusterWebHookConfiguration(TlsCertificateChainExport chainExport);
    }

    public class KubeWebHookConfigurationWriter : IKubeWebHookConfigurationWriter
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly TlsStorageOptions _tlsStorageOptions;
        private readonly MutatingWebHookOptions _mutatingWebHookOptions;
        private readonly IKubernetesClient _kubernetesClient;
        private readonly IResourcePatcher _resourcePatcher;

        public KubeWebHookConfigurationWriter(TlsStorageOptions tlsStorageOptions,
                                              MutatingWebHookOptions mutatingWebHookOptions,
                                              IKubernetesClient kubernetesClient,
                                              IResourcePatcher resourcePatcher)
        {
            _tlsStorageOptions = tlsStorageOptions;
            _mutatingWebHookOptions = mutatingWebHookOptions;
            _kubernetesClient = kubernetesClient;
            _resourcePatcher = resourcePatcher;
        }

        public async ValueTask<V1Secret?> FetchCurrentCertificate()
        {
            var existingSecret = await _kubernetesClient.Get<V1Secret>(_tlsStorageOptions.SecretName, _tlsStorageOptions.SecretNamespace);
            return existingSecret;
        }

        public async ValueTask UpdateClusterWebHookConfiguration(TlsCertificateChainExport chainExport)
        {
            await PublishCertificateSecret(chainExport);
            await UpdateWebHookConfiguration(chainExport);
        }

        private async ValueTask PublishCertificateSecret(TlsCertificateChainExport chainExport)
        {
            var (caCertificatePfx, caPublicPem, serverCertificatePfx) = chainExport;

            Logger.Info($"Ensuring certificates in '{_tlsStorageOptions.SecretNamespace}/{_tlsStorageOptions.SecretName}' are correct.");
            var secret = new V1Secret
            {
                Kind = V1Secret.KubeKind,
                Metadata = new V1ObjectMeta
                {
                    Name = _tlsStorageOptions.SecretName,
                    NamespaceProperty = _tlsStorageOptions.SecretNamespace,
                },
                Data = new Dictionary<string, byte[]>
                {
                    { _tlsStorageOptions.CaCertificateName, caCertificatePfx },
                    { _tlsStorageOptions.CaPublicName, caPublicPem },
                    { _tlsStorageOptions.ServerCertificateName, serverCertificatePfx }
                }
            };

            await _kubernetesClient.Save(secret);
        }

        private async ValueTask UpdateWebHookConfiguration(TlsCertificateChainExport chainExport)
        {
            Logger.Info($"Ensuring web hook ca bundle in '{_mutatingWebHookOptions.ConfigurationName}' is correct.");

            var success = await _resourcePatcher.Patch<V1MutatingWebhookConfiguration>(
                _mutatingWebHookOptions.ConfigurationName,
                null,
                configuration =>
                {
                    var webHook = configuration.Webhooks
                                               .FirstOrDefault(
                                                   x => string.Equals(x.Name, _mutatingWebHookOptions.WebHookName, StringComparison.OrdinalIgnoreCase)
                                               );
                    if (webHook != null)
                    {
                        webHook.ClientConfig.CaBundle = chainExport.CaPublicPem;
                    }
                }
            );

            if (!success)
            {
                Logger.Warn($"MutatingWebhookConfiguration '{_mutatingWebHookOptions.ConfigurationName}' "
                            + "was not found, web hooks will likely be broken.");
            }
        }
    }
}
