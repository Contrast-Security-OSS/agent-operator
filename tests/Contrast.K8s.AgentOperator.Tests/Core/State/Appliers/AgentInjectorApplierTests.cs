// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using Contrast.K8s.AgentOperator.Core.State;
using Contrast.K8s.AgentOperator.Core.State.Appliers;
using Contrast.K8s.AgentOperator.Core.State.Resources;
using Contrast.K8s.AgentOperator.Core.State.Resources.Primitives;
using Contrast.K8s.AgentOperator.Entities;
using FluentAssertions;
using k8s.Models;
using MediatR;
using NSubstitute;
using Xunit;

namespace Contrast.K8s.AgentOperator.Tests.Core.State.Appliers
{
    public class AgentInjectorApplierTests
    {
        private static readonly Fixture AutoFixture = new();

        [Theory]
        [InlineData("OnCreate", ReconcilePolicy.OnCreate)]
        [InlineData("Always", ReconcilePolicy.Always)]
        [InlineData(null, ReconcilePolicy.Always)]
        public async Task CreateFrom_maps_reconcile_policy(string? specValue, ReconcilePolicy expected)
        {
            var imageGenerator = Substitute.For<IImageGenerator>();
            imageGenerator.GenerateImage(
                    Arg.Any<AgentInjectionType>(),
                    Arg.Any<string?>(),
                    Arg.Any<string?>(),
                    Arg.Any<string?>(),
                    Arg.Any<CancellationToken>())
                .Returns(AutoFixture.Create<ContainerImageReference>());

            var applier = new AgentInjectorApplier(
                Substitute.For<IStateContainer>(),
                Substitute.For<IMediator>(),
                imageGenerator);

            var entity = new V1Beta1AgentInjector
            {
                Metadata = new V1ObjectMeta { Name = "inj", NamespaceProperty = "ns" },
                Spec = new V1Beta1AgentInjector.AgentInjectorSpec
                {
                    Type = "java",
                    ReconcilePolicy = specValue
                }
            };

            var resource = await applier.CreateFrom(entity);

            resource.ReconcilePolicy.Should().Be(expected);
        }
    }
}
