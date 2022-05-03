using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using KubeOps.Operator.Leadership;
using Microsoft.Extensions.Hosting;

namespace Contrast.K8s.AgentOperator.Core.Leading
{
    [UsedImplicitly]
    public class LeaderElectionWorker : BackgroundService
    {
        private readonly ILeaderElectionState _leaderElectionState;
        private readonly ILeaderElection _leaderElection;

        public LeaderElectionWorker(ILeaderElectionState leaderElectionState, ILeaderElection leaderElection)
        {
            _leaderElectionState = leaderElectionState;
            _leaderElection = leaderElection;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using (_leaderElection.LeadershipChange.Subscribe(OnNext))
            {
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
        }

        private async void OnNext(LeaderState state)
        {
            await _leaderElectionState.SetLeaderState(state);
        }
    }
}
