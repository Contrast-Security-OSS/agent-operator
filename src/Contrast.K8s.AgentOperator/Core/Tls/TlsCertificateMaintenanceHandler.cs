using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.Events;
using JetBrains.Annotations;
using k8s.Models;
using MediatR;
using NLog;

namespace Contrast.K8s.AgentOperator.Core.Tls
{
    [UsedImplicitly]
    public class TlsCertificateMaintenanceHandler : INotificationHandler<EntityReconciled<V1Secret>>, INotificationHandler<LeaderStateChanged>
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly IKestrelCertificateSelector _certificateSelector;
        private readonly ITlsCertificateChainGenerator _certificateChainGenerator;
        private readonly ITlsCertificateChainConverter _certificateChainConverter;
        private readonly IKubeWebHookConfigurationWriter _webHookConfigurationWriter;
        private readonly IWebHookSecretParser _webHookSecretParser;

        public TlsCertificateMaintenanceHandler(IKestrelCertificateSelector certificateSelector,
                                                ITlsCertificateChainGenerator certificateChainGenerator,
                                                ITlsCertificateChainConverter certificateChainConverter,
                                                IKubeWebHookConfigurationWriter webHookConfigurationWriter,
                                                IWebHookSecretParser webHookSecretParser)
        {
            _certificateSelector = certificateSelector;
            _certificateChainGenerator = certificateChainGenerator;
            _certificateChainConverter = certificateChainConverter;
            _webHookConfigurationWriter = webHookConfigurationWriter;
            _webHookSecretParser = webHookSecretParser;
        }

        public Task Handle(EntityReconciled<V1Secret> notification, CancellationToken cancellationToken)
        {
            if (_webHookSecretParser.TryGetWebHookCertificateSecret(notification.Entity, out var chain))
            {
                if (_certificateSelector.TakeOwnershipOfCertificate(chain))
                {
                    Logger.Info($"Secret '{notification.Entity.Namespace()}/{notification.Entity.Name()}' was changed, updated internal certificates.");
                }
                else
                {
                    chain.Dispose();
                }
            }

            return Task.CompletedTask;
        }

        public async Task Handle(LeaderStateChanged notification, CancellationToken cancellationToken)
        {
            if (notification.IsLeader)
            {
                var existingSecret = await _webHookConfigurationWriter.FetchCurrentCertificate();
                if (existingSecret == null)
                {
                    // Missing.
                    await GenerateAndPublishCertificate();
                }
                else if (_webHookSecretParser.TryGetWebHookCertificateSecret(existingSecret, out var chain))
                {
                    using (chain)
                    {
                        // Existing and valid, ensure web hook ca bundle is okay.
                        Logger.Info("Web hook certificate secret is valid.");
                        var chainExport = _certificateChainConverter.Export(chain);
                        await _webHookConfigurationWriter.UpdateClusterWebHookConfiguration(chainExport);
                    }
                }
                else
                {
                    // Invalid.
                    Logger.Info("Web hook certificate secret is invalid.");
                    await GenerateAndPublishCertificate();
                }
            }
        }

        private async Task GenerateAndPublishCertificate()
        {
            Logger.Info("Generating new certificates.");
            var stopwatch = Stopwatch.StartNew();

            using var chain = _certificateChainGenerator.CreateTlsCertificateChain();
            var chainExport = _certificateChainConverter.Export(chain);

            await _webHookConfigurationWriter.UpdateClusterWebHookConfiguration(chainExport);

            Logger.Info($"Completed generation after {stopwatch.ElapsedMilliseconds}ms.");
        }
    }
}
