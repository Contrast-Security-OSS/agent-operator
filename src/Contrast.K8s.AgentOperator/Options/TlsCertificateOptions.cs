using System;
using System.Collections.Generic;

namespace Contrast.K8s.AgentOperator.Options
{
    public record TlsCertificateOptions(string NamePrefix,
                                        IReadOnlyCollection<string> SanDnsNames,
                                        TimeSpan ExpiresAfter);
}
