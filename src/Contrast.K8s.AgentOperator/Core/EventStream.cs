using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using MediatR;
using NLog;

namespace Contrast.K8s.AgentOperator.Core
{
    public interface IEventStream
    {
        ValueTask Dispatch<T>(T request, CancellationToken cancellationToken = default) where T : IBaseRequest;
        ValueTask<IBaseRequest> DequeueNext(CancellationToken cancellationToken = default);
    }

    public class EventStream : IEventStream
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly Channel<IBaseRequest> _channel = Channel.CreateBounded<IBaseRequest>(CreateChannelOptions(), ItemDropped);

        public ValueTask Dispatch<T>(T request, CancellationToken cancellationToken = default) where T : IBaseRequest
        {
            return _channel.Writer.WriteAsync(request, cancellationToken);
        }

        public ValueTask<IBaseRequest> DequeueNext(CancellationToken cancellationToken = default)
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

        private static void ItemDropped(IBaseRequest obj)
        {
            Logger.Error("Unable to process events quick enough, dropping events for safety. Cluster snapshot will be out of date until restarting.");
        }
    }
}
