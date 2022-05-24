using System.Threading;

namespace Contrast.K8s.AgentOperator.Core.Telemetry.Models
{
    public record ExceptionReportWithOccurrences(ExceptionReport Report)
    {
        private int _occurrences;

        public int Occurrences => _occurrences;

        public void IncrementOccurrences()
        {
            Interlocked.Increment(ref _occurrences);
        }
    }
}
