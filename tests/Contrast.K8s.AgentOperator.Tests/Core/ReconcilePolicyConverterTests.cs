// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Contrast.K8s.AgentOperator.Core;
using Contrast.K8s.AgentOperator.Core.State.Resources.Primitives;
using FluentAssertions;
using Xunit;

namespace Contrast.K8s.AgentOperator.Tests.Core
{
    public class ReconcilePolicyConverterTests
    {
        [Theory]
        [InlineData("OnCreate", ReconcilePolicy.OnCreate)]
        [InlineData("oncreate", ReconcilePolicy.OnCreate)]
        [InlineData("Always", ReconcilePolicy.Always)]
        [InlineData("always", ReconcilePolicy.Always)]
        [InlineData(null, ReconcilePolicy.Always)]
        [InlineData("", ReconcilePolicy.Always)]
        [InlineData("nonsense", ReconcilePolicy.Always)]
        public void GetPolicyFromString_maps_as_expected(string? input, ReconcilePolicy expected)
        {
            ReconcilePolicyConverter.GetPolicyFromString(input).Should().Be(expected);
        }

        [Theory]
        [InlineData(ReconcilePolicy.OnCreate, "OnCreate")]
        [InlineData(ReconcilePolicy.Always, "Always")]
        public void GetStringFromPolicy_maps_as_expected(ReconcilePolicy input, string expected)
        {
            ReconcilePolicyConverter.GetStringFromPolicy(input).Should().Be(expected);
        }
    }
}
