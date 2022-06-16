// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.Events;
using Contrast.K8s.AgentOperator.Core.Leading;
using FluentAssertions;
using KubeOps.Operator.Leadership;
using MediatR;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Contrast.K8s.AgentOperator.Tests.Core.Leading
{
    public class LeaderElectionStateTests
    {
        [Fact]
        public void IsLeader_should_return_false_by_default()
        {
            var mediatorMock = Substitute.For<IMediator>();
            ILeaderElectionState state = new LeaderElectionState(mediatorMock);

            // Act
            var result = state.IsLeader();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task When_leader_state_changes_then_SetLeaderState_should_publish_state_changed_event()
        {
            var mediatorMock = Substitute.For<IMediator>();
            ILeaderElectionState state = new LeaderElectionState(mediatorMock);

            // Act
            await state.SetLeaderState(LeaderState.Leader);

            // Assert
            await mediatorMock.Received().Publish(Arg.Is(new LeaderStateChanged(true)));
        }

        [Fact]
        public async Task When_leader_state_changes_then_IsLeader_should_return_new_state()
        {
            var mediatorMock = Substitute.For<IMediator>();
            ILeaderElectionState state = new LeaderElectionState(mediatorMock);

            // Act
            await state.SetLeaderState(LeaderState.Leader);

            // Assert
            state.IsLeader().Should().BeTrue();
        }

        [Fact]
        public async Task When_LeaderStateChanged_publish_throws_then_exception_should_not_propagate()
        {
            var mediatorMock = Substitute.For<IMediator>();
            ILeaderElectionState state = new LeaderElectionState(mediatorMock);

            mediatorMock.Publish(Arg.Is(new LeaderStateChanged(true))).Throws(new Exception());

            // Act
            Func<Task> action = async () => await state.SetLeaderState(LeaderState.Leader);

            // Assert
            await action.Should().NotThrowAsync();
        }
    }
}
