// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Threading;

namespace Contrast.K8s.AgentOperator.Core.Telemetry.Models;

public record ExceptionReportWithOccurrences(ExceptionReport Report)
{
    private int _occurrences;

    public int Occurrences => _occurrences;

    public void IncrementOccurrences()
    {
        Interlocked.Increment(ref _occurrences);
    }
}
