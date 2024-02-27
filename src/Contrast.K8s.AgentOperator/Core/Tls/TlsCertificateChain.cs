// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Security.Cryptography.X509Certificates;

namespace Contrast.K8s.AgentOperator.Core.Tls;

public record TlsCertificateChain(X509Certificate2 CaCertificate,
                                  X509Certificate2 ServerCertificate,
                                  byte[] SanDnsNamesHash,
                                  byte[] Version) : IDisposable
{
    public void Dispose()
    {
        CaCertificate.Dispose();
        ServerCertificate.Dispose();
    }
}
