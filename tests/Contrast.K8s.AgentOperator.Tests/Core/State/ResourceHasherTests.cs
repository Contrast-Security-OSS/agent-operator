using AutoFixture;
using Contrast.K8s.AgentOperator.Core.Kube;
using Contrast.K8s.AgentOperator.Core.State;
using Contrast.K8s.AgentOperator.Core.State.Resources;
using FluentAssertions;
using Xunit;

namespace Contrast.K8s.AgentOperator.Tests.Core.State
{
    public class ResourceHasherTests
    {
        private static readonly Fixture AutoFixture = new();

        [Fact]
        public void GetHash_should_return_a_valid_hash()
        {
            var agentInjectorResourceFake = AutoFixture.Create<AgentInjectorResource>();
            var agentConnectionResourceFake = AutoFixture.Create<AgentConnectionResource>();
            var agentConfigurationResourceFake = AutoFixture.Create<AgentConfigurationResource>();
            var secretResourcesFake = AutoFixture.CreateMany<SecretResource>();

            var hasher = CreateGraph();

            // Act
            var result = hasher.GetHash(agentInjectorResourceFake, agentConnectionResourceFake, agentConfigurationResourceFake, secretResourcesFake);

            // Assert
            result.Should().NotBeNullOrWhiteSpace();
        }

        private static IResourceHasher CreateGraph(KubernetesJsonSerializer? jsonSerializer = null)
        {
            return new ResourceHasher(jsonSerializer ?? new KubernetesJsonSerializer());
        }
    }
}
