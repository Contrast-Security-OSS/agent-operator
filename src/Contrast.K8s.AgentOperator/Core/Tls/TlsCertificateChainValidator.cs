// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;

namespace Contrast.K8s.AgentOperator.Core.Tls
{
    public interface ITlsCertificateChainValidator
    {
        bool IsValid(TlsCertificateChain chain, out ValidationResultReason reason);
    }

    public class TlsCertificateChainValidator : ITlsCertificateChainValidator
    {
        public bool IsValid(TlsCertificateChain chain, out ValidationResultReason reason)
        {
            var renewThreshold = DateTime.Now + TimeSpan.FromDays(90);

            if (!chain.CaCertificate.HasPrivateKey
                || !chain.ServerCertificate.HasPrivateKey)
            {
                reason = ValidationResultReason.MissingPrivateKey;
            }
            else if (chain.CaCertificate.NotAfter < renewThreshold
                     || chain.ServerCertificate.NotAfter < renewThreshold)
            {
                reason = ValidationResultReason.Expired;
            }
            else if (!TlsCertificateChainGenerator.GenerationVersion.SequenceEqual(chain.Version))
            {
                reason = ValidationResultReason.OldVersion;
            }
            else
            {
                reason = ValidationResultReason.NoError;
            }

            return reason == ValidationResultReason.NoError;
        }
    }

    public enum ValidationResultReason
    {
        NoError = 0,
        MissingPrivateKey,
        Expired,
        OldVersion
    }
}
