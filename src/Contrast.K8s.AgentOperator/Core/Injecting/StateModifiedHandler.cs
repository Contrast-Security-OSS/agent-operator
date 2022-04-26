using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.Events;
using Contrast.K8s.AgentOperator.Core.State;
using MediatR;
using NLog;

namespace Contrast.K8s.AgentOperator.Core.Injecting
{
    public class StateModifiedHandler : INotificationHandler<StateModified>
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly IStateContainer _state;
        private readonly IAgentInjector _agentInjector;

        public StateModifiedHandler(IStateContainer state, IAgentInjector agentInjector)
        {
            _state = state;
            _agentInjector = agentInjector;
        }

        public async Task Handle(StateModified notification, CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            Logger.Trace("Cluster state changed, re-calculating injection points.");

            await _agentInjector.CalculateChanges(cancellationToken);

            Logger.Trace($"Completed re-calculating injection points after {stopwatch.ElapsedMilliseconds}ms.");
        }
    }
}
