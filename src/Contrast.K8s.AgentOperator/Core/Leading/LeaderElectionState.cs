using System;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.Events;
using KubeOps.Operator.Leadership;
using MediatR;
using NLog;

namespace Contrast.K8s.AgentOperator.Core.Leading
{
    public interface ILeaderElectionState
    {
        bool IsLeader();
        Task SetLeaderState(LeaderState state);
    }

    public class LeaderElectionState : ILeaderElectionState
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly IMediator _mediator;
        private LeaderState _state = LeaderState.None;

        public LeaderElectionState(IMediator mediator)
        {
            _mediator = mediator;
        }

        public bool IsLeader()
        {
            return _state == LeaderState.Leader;
        }

        public async Task SetLeaderState(LeaderState state)
        {
            var lastState = _state;
            if (lastState != state)
            {
                _state = state;

                try
                {
                    await _mediator.Publish(new LeaderStateChanged(IsLeader()));
                }
                catch (Exception e)
                {
                    Logger.Warn(e);
                }
            }
        }
    }
}
