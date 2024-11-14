// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.Telemetry;
using Contrast.K8s.AgentOperator.Core.Telemetry.Services.Metrics;
using Microsoft.AspNetCore.Mvc;

namespace Contrast.K8s.AgentOperator.Controllers;

[ApiController, Route("api/v1/metrics")]
public class MetricsController : Controller
{
    private readonly StatusReportGenerator _reportGenerator;
    private readonly DefaultTagsFactory _defaultTagsFactory;

    public MetricsController(StatusReportGenerator reportGenerator, DefaultTagsFactory defaultTagsFactory)
    {
        _reportGenerator = reportGenerator;
        _defaultTagsFactory = defaultTagsFactory;
    }

    [HttpGet]
    public async Task<IActionResult> GetMetrics(CancellationToken cancellationToken = default)
    {
        var report = await _reportGenerator.Generate(cancellationToken);

        // Combine the Values and Tags from the report into a single output
        var output = new Dictionary<string, object>();

        foreach (var value in report.Values)
        {
            output.Add(value.Key, value.Value);
        }

        foreach (var tag in report.ExtraTags)
        {
            output.Add(tag.Key, tag.Value);
        }

        return Ok(output);
    }

    [HttpGet("tags")]
    public async Task<IActionResult> GetTags(CancellationToken cancellationToken = default)
    {
        var tags = await _defaultTagsFactory.GetDefaultTags();
        return Ok(tags);
    }
}
