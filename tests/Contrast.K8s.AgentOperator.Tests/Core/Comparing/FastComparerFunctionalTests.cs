// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using AutoFixture;
using AutoFixture.Kernel;
using Contrast.K8s.AgentOperator.Core.Comparing;
using Contrast.K8s.AgentOperator.Core.State.Resources;
using FluentAssertions;
using Newtonsoft.Json;
using Xunit;

namespace Contrast.K8s.AgentOperator.Tests.Core.Comparing
{
    public class FastComparerFunctionalTests
    {
        private static readonly Fixture AutoFixture = new();

        [Theory]
        [InlineData(typeof(AgentConfigurationResource))]
        [InlineData(typeof(AgentConnectionResource))]
        [InlineData(typeof(AgentInjectorResource))]
        [InlineData(typeof(ClusterAgentConfigurationResource))]
        [InlineData(typeof(ClusterAgentConnectionResource))]
        [InlineData(typeof(DaemonSetResource))]
        [InlineData(typeof(DeploymentConfigResource))]
        [InlineData(typeof(DeploymentResource))]
        [InlineData(typeof(PodResource))]
        [InlineData(typeof(SecretResource))]
        [InlineData(typeof(StatefulSetResource))]
        public void When_objects_are_the_same_then_AreEqual_should_return_true(Type type)
        {
            var leftFake = FakeType(type);
            var rightFake = DeepClone(leftFake, type);

            var comparer = new FastComparer(new ObjectComparerPlanner());

            // Act
            var result = comparer.AreEqual(leftFake, rightFake);

            // Assert
            result.Should().BeTrue();
        }

        [Theory]
        [InlineData(typeof(AgentConfigurationResource))]
        [InlineData(typeof(AgentConnectionResource))]
        [InlineData(typeof(AgentInjectorResource))]
        [InlineData(typeof(ClusterAgentConfigurationResource))]
        [InlineData(typeof(ClusterAgentConnectionResource))]
        [InlineData(typeof(DaemonSetResource))]
        [InlineData(typeof(DeploymentConfigResource))]
        [InlineData(typeof(DeploymentResource))]
        [InlineData(typeof(PodResource))]
        [InlineData(typeof(SecretResource))]
        [InlineData(typeof(StatefulSetResource))]
        public void When_objects_are_different_then_AreEqual_should_return_false(Type type)
        {
            var leftFake = FakeType(type);
            var rightFake = FakeType(type);

            var comparer = new FastComparer(new ObjectComparerPlanner());

            // Act
            var result = comparer.AreEqual(leftFake, rightFake);

            // Assert
            result.Should().BeFalse();
        }

        [return: NotNullIfNotNull("obj")]
        private static object? DeepClone(object? obj, Type type)
        {
            return obj == null
                ? obj
                : JsonConvert.DeserializeObject(JsonConvert.SerializeObject(obj), type)!;
        }

        private static object FakeType(Type type)
        {
            return AutoFixture.Create(type, new SpecimenContext(AutoFixture));
        }
    }
}
