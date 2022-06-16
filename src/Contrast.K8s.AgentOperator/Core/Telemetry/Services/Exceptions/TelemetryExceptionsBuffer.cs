// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.Telemetry.Models;

namespace Contrast.K8s.AgentOperator.Core.Telemetry.Services.Exceptions
{
    public class TelemetryExceptionsBuffer
    {
        private readonly Channel<ExceptionReport> _channel = Channel.CreateBounded<ExceptionReport>(new BoundedChannelOptions(128)
        {
            FullMode = BoundedChannelFullMode.DropWrite
        });

        public static TelemetryExceptionsBuffer Instance { get; } = new();

        private TelemetryExceptionsBuffer()
        {
        }

        public ValueTask Add(ExceptionReport report, CancellationToken cancellationToken = default)
        {
            return _channel.Writer.WriteAsync(report, cancellationToken);
        }

        public ValueTask<ExceptionReport> Take(CancellationToken cancellationToken = default)
        {
            return _channel.Reader.ReadAsync(cancellationToken);
        }
    }
}
