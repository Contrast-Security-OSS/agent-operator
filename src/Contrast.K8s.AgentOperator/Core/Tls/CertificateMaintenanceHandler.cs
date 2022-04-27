using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.Events;
using Contrast.K8s.AgentOperator.Options;
using JetBrains.Annotations;
using k8s.Models;
using MediatR;
using NLog;

namespace Contrast.K8s.AgentOperator.Core.Tls
{
    [UsedImplicitly]
    public class CertificateMaintenanceHandler : INotificationHandler<EntityReconciled<V1Secret>>, INotificationHandler<NowLeader>
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly IKestrelCertificateSelector _certificateSelector;
        private readonly TlsStorageOptions _tlsStorageOptions;
        private readonly TlsCertificateChainGenerator _certificateChainGenerator;
        private readonly ITlsCertificateChainConverter _certificateChainConverter;
        private readonly IKubeWebHookConfigurationWriter _webHookConfigurationWriter;

        public CertificateMaintenanceHandler(IKestrelCertificateSelector certificateSelector,
                                             TlsStorageOptions tlsStorageOptions,
                                             TlsCertificateChainGenerator certificateChainGenerator,
                                             ITlsCertificateChainConverter certificateChainConverter,
                                             IKubeWebHookConfigurationWriter webHookConfigurationWriter)
        {
            _certificateSelector = certificateSelector;
            _tlsStorageOptions = tlsStorageOptions;
            _certificateChainGenerator = certificateChainGenerator;
            _certificateChainConverter = certificateChainConverter;
            _webHookConfigurationWriter = webHookConfigurationWriter;
        }

        public Task Handle(EntityReconciled<V1Secret> notification, CancellationToken cancellationToken)
        {
            if (TryGetWebHookCertificateSecret(notification.Entity, out var chain))
            {
                Logger.Info($"Secret '{notification.Entity.Namespace()}/{notification.Entity.Name()}' was changed, updating internal certificate.");
                _certificateSelector.SetCertificate(chain);
            }

            return Task.CompletedTask;
        }

        public async Task Handle(NowLeader notification, CancellationToken cancellationToken)
        {
            var existingSecret = await _webHookConfigurationWriter.FetchCurrentCertificate();
            if (existingSecret == null)
            {
                // Missing.
                await GenerateAndPublishCertificate();
            }
            else if (TryGetWebHookCertificateSecret(existingSecret, out var chain))
            {
                // Existing and valid, ensure web hook ca bundle is okay.
                Logger.Info($"Web hook certificate secret '{_tlsStorageOptions.SecretNamespace}/{_tlsStorageOptions.SecretName}' is valid.");
                var chainExport = _certificateChainConverter.Export(chain);
                await _webHookConfigurationWriter.UpdateClusterWebHookConfiguration(chainExport);
            }
            else
            {
                // Invalid.
                Logger.Info($"Web hook certificate secret '{_tlsStorageOptions.SecretNamespace}/{_tlsStorageOptions.SecretName}' is invalid.");
                await GenerateAndPublishCertificate();
            }
        }

        private async Task GenerateAndPublishCertificate()
        {
            Logger.Info($"Generating new certificates to be stored in '{_tlsStorageOptions.SecretNamespace}/{_tlsStorageOptions.SecretName}'.");
            var stopwatch = Stopwatch.StartNew();

            var chain = _certificateChainGenerator.CreateTlsCertificateChain();
            var chainExport = _certificateChainConverter.Export(chain);

            await _webHookConfigurationWriter.UpdateClusterWebHookConfiguration(chainExport);

            Logger.Info($"Completed generation after {stopwatch.ElapsedMilliseconds}ms.");
        }

        private bool TryGetWebHookCertificateSecret(V1Secret secret, [NotNullWhen(true)] out TlsCertificateChain? chain)
        {
            // TODO Check case sensitive logic.
            if (secret.Name() == _tlsStorageOptions.SecretName
                && string.Equals(secret.Namespace(), _tlsStorageOptions.SecretNamespace, StringComparison.OrdinalIgnoreCase))
            {
                if (secret.Data.TryGetValue(_tlsStorageOptions.ServerCertificateName, out var serverCertificateBytes)
                    && secret.Data.TryGetValue(_tlsStorageOptions.CaPublicName, out var caPublicBytes)
                    && secret.Data.TryGetValue(_tlsStorageOptions.CaCertificateName, out var caCertificateBytes))
                {
                    try
                    {
                        chain = _certificateChainConverter.Import(new TlsCertificateChainExport(caCertificateBytes, caPublicBytes, serverCertificateBytes));

                        var renewThreshold = DateTime.Now + TimeSpan.FromDays(90);
                        return chain.CaCertificate.HasPrivateKey
                               && chain.ServerCertificate.HasPrivateKey
                               && chain.CaCertificate.NotAfter > renewThreshold
                               && chain.ServerCertificate.NotAfter > renewThreshold;
                    }
                    catch (Exception e)
                    {
                        Logger.Trace(e);
                    }
                }
            }

            chain = default;
            return false;
        }
    }
}
