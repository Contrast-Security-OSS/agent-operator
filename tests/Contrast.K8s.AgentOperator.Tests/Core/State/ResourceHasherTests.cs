// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Linq;
using AutoFixture;
using Contrast.K8s.AgentOperator.Core.Kube;
using Contrast.K8s.AgentOperator.Core.State;
using Contrast.K8s.AgentOperator.Core.State.Resources;
using Contrast.K8s.AgentOperator.Core.State.Resources.Primitives;
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

        [Fact]
        public void GetHash_still_changes_when_a_hashed_field_changes()
        {
            var injector = AutoFixture.Create<AgentInjectorResource>() with { Enabled = true };
            var flipped = injector with { Enabled = false };
            var connection = AutoFixture.Create<AgentConnectionResource>();
            var configuration = AutoFixture.Create<AgentConfigurationResource>();
            var secrets = AutoFixture.CreateMany<SecretResource>().ToList();
            var hasher = CreateGraph();

            var original = hasher.GetHash(injector, connection, configuration, secrets);
            var changed = hasher.GetHash(flipped, connection, configuration, secrets);

            changed.Should().NotBe(original);
        }

        private static IResourceHasher CreateGraph(KubernetesJsonSerializer? jsonSerializer = null)
        {
            return new ResourceHasher(jsonSerializer ?? new KubernetesJsonSerializer());
        }
    }
}
