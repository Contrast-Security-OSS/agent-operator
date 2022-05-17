using System.Threading;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.Tls;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Contrast.K8s.AgentOperator
{
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
                result = HealthCheckResult.Unhealthy("No valid certificate has been loaded.");
            }

            return Task.FromResult(result);
        }
    }
}
