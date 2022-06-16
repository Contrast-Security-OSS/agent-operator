// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using Contrast.K8s.AgentOperator.Options;
using k8s.Models;
using NLog;

namespace Contrast.K8s.AgentOperator.Core.Tls
{
    public interface IWebHookSecretParser
    {
        bool TryGetWebHookCertificateSecret(V1Secret secret, [NotNullWhen(true)] out TlsCertificateChain? chain);
    }

    public class WebHookSecretParser : IWebHookSecretParser
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly TlsStorageOptions _tlsStorageOptions;
        private readonly ITlsCertificateChainConverter _certificateChainConverter;

        public WebHookSecretParser(TlsStorageOptions tlsStorageOptions,
                                   ITlsCertificateChainConverter certificateChainConverter)
        {
            _tlsStorageOptions = tlsStorageOptions;
            _certificateChainConverter = certificateChainConverter;
        }

        public bool TryGetWebHookCertificateSecret(V1Secret secret, [NotNullWhen(true)] out TlsCertificateChain? chain)
        {
            // TODO Check case sensitive logic.
            if (secret.Name() == _tlsStorageOptions.SecretName
                && string.Equals(secret.Namespace(), _tlsStorageOptions.SecretNamespace, StringComparison.OrdinalIgnoreCase)
                && secret.Data != null
                && secret.Data.TryGetValue(_tlsStorageOptions.ServerCertificateName, out var serverCertificateBytes)
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

            chain = default;
            return false;
        }
    }
}
