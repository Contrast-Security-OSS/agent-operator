// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.Telemetry;
using Contrast.K8s.AgentOperator.Core.Telemetry.Services.Metrics;
using Microsoft.AspNetCore.Mvc;
using NLog;

namespace Contrast.K8s.AgentOperator.Controllers;

[ApiController, Route("api/v1/metrics")]
public class MetricsController : Controller
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

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

        foreach (var value in report.Values.OrderBy(r => r.Key))
        {
            output.Add(value.Key, value.Value);
        }

        foreach (var stat in GenerateProcessingStatistics())
        {
            output.Add(stat.Key, stat.Value);
        }

        foreach (var tag in report.ExtraTags.OrderBy(r => r.Key))
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

    private IDictionary<string, object> GenerateProcessingStatistics()
    {
        var statistics = new Dictionary<string, object>();

        try
        {
            var currentProcess = Process.GetCurrentProcess();

            // Do them in groups because they can all throw InvalidOperationException/NotSupportedException
            // Makes the code messier, but it means we'll still get as many as we can if some don't work
            try
            {
                statistics.Add("Process.WorkingSet64", currentProcess.WorkingSet64);
                statistics.Add("Process.MinWorkingSet", currentProcess.MinWorkingSet.ToInt64());
                statistics.Add("Process.MaxWorkingSet", currentProcess.MaxWorkingSet.ToInt64());
                statistics.Add("Process.PeakWorkingSet64", currentProcess.PeakWorkingSet64);
            }
            catch (Exception ex)
            {
                Logger.Warn(ex);
            }

            try
            {
                statistics.Add("Process.PrivateMemorySize64", currentProcess.PrivateMemorySize64);
                statistics.Add("Process.VirtualMemorySize64", currentProcess.VirtualMemorySize64);
                statistics.Add("Process.PeakVirtualMemorySize64", currentProcess.PeakVirtualMemorySize64);
            }
            catch (Exception ex)
            {
                Logger.Warn(ex);
            }

            try
            {
                statistics.Add("Process.PagedMemorySize64", currentProcess.PagedMemorySize64);
                statistics.Add("Process.PeakPagedMemorySize64", currentProcess.PeakPagedMemorySize64);
                statistics.Add("Process.NonpagedSystemMemorySize64", currentProcess.NonpagedSystemMemorySize64);
            }
            catch (Exception ex)
            {
                Logger.Warn(ex);
            }

            try
            {
                statistics.Add("Process.TotalProcessorTime", currentProcess.TotalProcessorTime);
                statistics.Add("Process.UserProcessorTime", currentProcess.UserProcessorTime);
                statistics.Add("Process.PrivilegedProcessorTime", currentProcess.PrivilegedProcessorTime);
            }
            catch (Exception ex)
            {
                Logger.Warn(ex);
            }

            try
            {
                statistics.Add("Process.Thread", currentProcess.Threads.Count);
                statistics.Add("Process.Modules", currentProcess.Modules.Count);
            }
            catch (Exception ex)
            {
                Logger.Warn(ex);
            }
        }
        catch (Exception ex)
        {
            Logger.Warn(ex);
        }

        return statistics;
    }
}
