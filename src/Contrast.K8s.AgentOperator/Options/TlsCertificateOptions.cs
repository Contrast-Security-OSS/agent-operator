// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Contrast.K8s.AgentOperator.Options
{
    public record TlsCertificateOptions(string NamePrefix,
                                        IReadOnlyCollection<string> SanDnsNames,
                                        TimeSpan ExpiresAfter);
}
