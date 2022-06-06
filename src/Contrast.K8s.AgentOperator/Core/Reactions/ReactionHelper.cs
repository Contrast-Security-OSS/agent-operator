using System.Threading;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.Leading;
using Contrast.K8s.AgentOperator.Core.State;

namespace Contrast.K8s.AgentOperator.Core.Reactions
{
    public interface IReactionHelper
    {
        ValueTask<bool> CanReact(CancellationToken cancellationToken = default);
    }

    public class ReactionHelper : IReactionHelper
    {
        private readonly ILeaderElectionState _electionState;
        private readonly IStateContainer _state;

        public ReactionHelper(ILeaderElectionState electionState, IStateContainer state)
        {
            _electionState = electionState;
            _state = state;
        }

        public async ValueTask<bool> CanReact(CancellationToken cancellationToken = default)
        {
            return _electionState.IsLeader()
                   && await _state.GetHasSettled(cancellationToken);
        }
    }
}
