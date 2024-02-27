// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.Events;
using JetBrains.Annotations;
using k8s.Models;
using MediatR;
using NLog;

namespace Contrast.K8s.AgentOperator.Core.Tls;

[UsedImplicitly]
public class TlsCertificateMaintenanceHandler : INotificationHandler<EntityReconciled<V1Secret>>, INotificationHandler<LeaderStateChanged>
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly IKestrelCertificateSelector _certificateSelector;
    private readonly ITlsCertificateChainGenerator _certificateChainGenerator;
    private readonly ITlsCertificateChainConverter _certificateChainConverter;
    private readonly IKubeWebHookConfigurationWriter _webHookConfigurationWriter;
    private readonly IWebHookSecretParser _webHookSecretParser;
    private readonly ITlsCertificateChainValidator _validator;

    public TlsCertificateMaintenanceHandler(IKestrelCertificateSelector certificateSelector,
                                            ITlsCertificateChainGenerator certificateChainGenerator,
                                            ITlsCertificateChainConverter certificateChainConverter,
                                            IKubeWebHookConfigurationWriter webHookConfigurationWriter,
                                            IWebHookSecretParser webHookSecretParser,
                                            ITlsCertificateChainValidator validator)
    {
        _certificateSelector = certificateSelector;
        _certificateChainGenerator = certificateChainGenerator;
        _certificateChainConverter = certificateChainConverter;
        _webHookConfigurationWriter = webHookConfigurationWriter;
        _webHookSecretParser = webHookSecretParser;
        _validator = validator;
    }

    public Task Handle(EntityReconciled<V1Secret> notification, CancellationToken cancellationToken)
    {
        if (_webHookSecretParser.TryGetWebHookCertificateSecret(notification.Entity, out var chain))
        {
            // The certificate may not be valid, but that's not for us to figure out right now.
            // At this point, we only care if the certificate was parseable.
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
            if (existingSecret == null
                || !_webHookSecretParser.TryGetWebHookCertificateSecret(existingSecret, out var chain))
            {
                // Missing.
                await GenerateAndPublishCertificate();
            }
            else
            {
                using (chain)
                {
                    if (_validator.IsValid(chain, out var reason))
                    {
                        // Existing and valid, ensure web hook ca bundle is okay.
                        Logger.Info("Web hook certificate secret is valid.");
                        var chainExport = _certificateChainConverter.Export(chain);
                        await _webHookConfigurationWriter.UpdateClusterWebHookConfiguration(chainExport);
                    }
                    else
                    {
                        // Invalid.
                        Logger.Info($"Web hook certificate secret is invalid (Reason: '{reason}').");
                        await GenerateAndPublishCertificate();
                    }
                }
            }
        }
    }

    private async ValueTask GenerateAndPublishCertificate()
    {
        Logger.Info("Generating new certificates.");
        var stopwatch = Stopwatch.StartNew();

        using var chain = _certificateChainGenerator.CreateTlsCertificateChain();
        var chainExport = _certificateChainConverter.Export(chain);

        await _webHookConfigurationWriter.UpdateClusterWebHookConfiguration(chainExport);

        Logger.Info($"Completed generation after {stopwatch.ElapsedMilliseconds}ms.");
    }
}
