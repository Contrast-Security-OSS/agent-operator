// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.Telemetry.Services.Metrics;
using Microsoft.AspNetCore.Mvc;

namespace Contrast.K8s.AgentOperator.Controllers
{
    [ApiController, Route("api/v1/metrics")]
    public class MetricsController : Controller
    {
        private readonly StatusReportGenerator _reportGenerator;

        public MetricsController(StatusReportGenerator reportGenerator)
        {
            _reportGenerator = reportGenerator;
        }

        [HttpGet]
        public async Task<IActionResult> GetReport(CancellationToken cancellationToken = default)
        {
            var report = await _reportGenerator.Generate(cancellationToken);
            return Ok(report.Values);
        }
    }
}
