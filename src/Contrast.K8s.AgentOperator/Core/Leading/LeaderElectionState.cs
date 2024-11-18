// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.Events;
using k8s.Autorest;
using k8s.LeaderElection;
using MediatR;
using NLog;

namespace Contrast.K8s.AgentOperator.Core.Leading;

public interface ILeaderElectionState
{
    bool IsLeader();
}

public class LeaderElectionState : ILeaderElectionState
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly IMediator _mediator;
    private readonly LeaderElector _elector;

    public LeaderElectionState(IMediator mediator, LeaderElector elector)
    {
        _mediator = mediator;
        _elector = elector;

        elector.OnStartedLeading += StartedLeading;
        elector.OnStoppedLeading += StoppedLeading;
    }

    private async void StartedLeading()
    {
        Logger.Info("This instance leadership state has changed to leader.");

        await LeadershipChange();
    }

    private async void StoppedLeading()
    {
        Logger.Info("This instance leadership state has changed to non-leader.");

        await LeadershipChange();
    }

    private async Task LeadershipChange()
    {
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

    public bool IsLeader()
    {
        return _elector.IsLeader();
    }
}
