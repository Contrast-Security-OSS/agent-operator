// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using MediatR;
using NLog;

namespace Contrast.K8s.AgentOperator.Core.State
{
    public interface IEventStream
    {
        ValueTask DispatchDeferred<T>(T request, CancellationToken cancellationToken = default) where T : INotification;
        ValueTask<INotification> DequeueNext(CancellationToken cancellationToken = default);
    }

    public class EventStream : IEventStream
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly Channel<INotification> _channel = Channel.CreateBounded<INotification>(CreateChannelOptions(), ItemDropped);

        public ValueTask DispatchDeferred<T>(T request, CancellationToken cancellationToken = default) where T : INotification
        {
            return _channel.Writer.WriteAsync(request, cancellationToken);
        }

        public ValueTask<INotification> DequeueNext(CancellationToken cancellationToken = default)
        {
            return _channel.Reader.ReadAsync(cancellationToken);
        }

        private static BoundedChannelOptions CreateChannelOptions()
        {
            return new BoundedChannelOptions(10 * 1024)
            {
                FullMode = BoundedChannelFullMode.DropOldest
            };
        }

        private static void ItemDropped(INotification obj)
        {
            Logger.Error(
                "Unable to process events quick enough, dropping events for safety. Cluster snapshot will be out of date until this operator is restarted.");
        }
    }
}
