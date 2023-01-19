// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.Events;
using k8s.Autorest;
using KubeOps.Operator.Leadership;
using MediatR;
using NLog;

namespace Contrast.K8s.AgentOperator.Core.Leading
{
    public interface ILeaderElectionState
    {
        bool IsLeader();
        ValueTask SetLeaderState(LeaderState state);
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

        public async ValueTask SetLeaderState(LeaderState state)
        {
            var lastState = _state;
            if (lastState != state)
            {
                _state = state;

                Logger.Info($"This instance leadership state has changed '{lastState}' -> '{state}'.");

                try
                {
                    await _mediator.Publish(new LeaderStateChanged(IsLeader()));
                }
                catch (HttpOperationException e)
                {
                    Logger.Warn(e, $"An error occurred. Response body: '{e.Response.Content}'.");
                }
                catch (Exception e)
                {
                    Logger.Warn(e);
                }
            }
        }
    }
}
