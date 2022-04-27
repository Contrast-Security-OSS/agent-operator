using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.Events;
using Contrast.K8s.AgentOperator.Options;
using DotnetKubernetesClient;
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
        private readonly TlsCertificateOptions _tlsCertificateOptions;
        private readonly TlsStorageOptions _tlsStorageOptions;
        private readonly TlsCertificateChainGenerator _certificateChainGenerator;
        private readonly ITlsCertificateChainConverter _certificateChainConverter;
        private readonly IKubernetesClient _kubernetesClient;

        public CertificateMaintenanceHandler(IKestrelCertificateSelector certificateSelector,
                                             TlsCertificateOptions tlsCertificateOptions,
                                             TlsStorageOptions tlsStorageOptions,
                                             TlsCertificateChainGenerator certificateChainGenerator,
                                             ITlsCertificateChainConverter certificateChainConverter,
                                             IKubernetesClient kubernetesClient)
        {
            _certificateSelector = certificateSelector;
            _tlsCertificateOptions = tlsCertificateOptions;
            _tlsStorageOptions = tlsStorageOptions;
            _certificateChainGenerator = certificateChainGenerator;
            _certificateChainConverter = certificateChainConverter;
            _kubernetesClient = kubernetesClient;
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
            var existingSecret = await _kubernetesClient.Get<V1Secret>(_tlsStorageOptions.SecretName, _tlsStorageOptions.SecretNamespace);
            if (existingSecret != null)
            {
                Logger.Info($"Web hook certificate secret '{_tlsStorageOptions.SecretNamespace}/{_tlsStorageOptions.SecretName}' exists, will validate.");
            }

            if (existingSecret == null || !TryGetWebHookCertificateSecret(existingSecret, out _))
            {
                Logger.Info($"Generating new certificates to be stored in '{_tlsStorageOptions.SecretNamespace}/{_tlsStorageOptions.SecretName}'.");
                var stopwatch = Stopwatch.StartNew();

                var chain = _certificateChainGenerator.CreateTlsCertificateChain(_tlsCertificateOptions);
                var (caCertificatePem, caPublicPem, serverCertificatePem) = _certificateChainConverter.Export(chain);

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
                        { _tlsStorageOptions.CaCertificateName, caCertificatePem },
                        { _tlsStorageOptions.CaPublicName, caPublicPem },
                        { _tlsStorageOptions.ServerCertificateName, serverCertificatePem }
                    }
                };

                await _kubernetesClient.Save(secret);

                Logger.Info($"Completed generation after {stopwatch.ElapsedMilliseconds}ms.");
            }
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
                        return chain.ServerCertificate.HasPrivateKey;
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
