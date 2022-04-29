using System;
using System.Threading;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.Events;
using JetBrains.Annotations;
using KubeOps.Operator.Leadership;
using MediatR;
using Microsoft.Extensions.Hosting;
using NLog;

namespace Contrast.K8s.AgentOperator.Core.Workers
{
    [UsedImplicitly]
    public class LeaderElectionWorker : BackgroundService
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly IMediator _mediator;
        private readonly ILeaderElection _leaderElection;

        public LeaderElectionWorker(IMediator mediator, ILeaderElection leaderElection)
        {
            _mediator = mediator;
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
            Logger.Trace($"This operator leader state changed to '{state}'.");
            if (state == LeaderState.Leader)
            {
                try
                {
                    await _mediator.Publish(new ElectedLeader());
                }
                catch (Exception e)
                {
                    Logger.Warn(e);
                }
            }
        }
    }
}
