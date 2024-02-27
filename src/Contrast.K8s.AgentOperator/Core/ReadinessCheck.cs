// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.Tls;
using JetBrains.Annotations;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Contrast.K8s.AgentOperator.Core;

[UsedImplicitly]
public class ReadinessCheck : IHealthCheck
{
    private readonly IKestrelCertificateSelector _certificateSelector;

    public ReadinessCheck(IKestrelCertificateSelector certificateSelector)
    {
        _certificateSelector = certificateSelector;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var result = HealthCheckResult.Healthy();
        if (!_certificateSelector.HasValidCertificate())
        {
            result = HealthCheckResult.Unhealthy("No valid certificate has been loaded. If this warning continues to occur, a permission problem may be present.");
        }

        return Task.FromResult(result);
    }
}
