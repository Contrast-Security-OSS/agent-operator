using System;
using System.Security.Cryptography.X509Certificates;
using NLog;

namespace Contrast.K8s.AgentOperator.Core.Tls
{
    public interface IKestrelCertificateSelector : IDisposable
    {
        X509Certificate2? SelectCertificate(string? hostname);
        bool TakeOwnershipCertificate(TlsCertificateChain certificate);
    }

    public class KestrelCertificateSelector : IKestrelCertificateSelector
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private TlsCertificateChain? _chain;

        public X509Certificate2? SelectCertificate(string? hostname)
        {
            if (_chain == null)
            {
                Logger.Warn($"A server certificate was requested for '{hostname}', but none was known at this time.");
            }

            return _chain?.ServerCertificate;
        }

        public bool TakeOwnershipCertificate(TlsCertificateChain chain)
        {
            var ownershipTaken = chain.ServerCertificate.SerialNumber != _chain?.ServerCertificate.SerialNumber
                                 || chain.CaCertificate.SerialNumber != _chain?.CaCertificate.SerialNumber;

            if (ownershipTaken)
            {
                _chain?.Dispose();
                _chain = chain;
            }

            return ownershipTaken;
        }

        public void Dispose()
        {
            _chain?.Dispose();
        }
    }
}
