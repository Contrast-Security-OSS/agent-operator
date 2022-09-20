// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json;
using Contrast.K8s.AgentOperator.Entities.Dynatrace;
using FluentAssertions;
using Xunit;

namespace Contrast.K8s.AgentOperator.Tests.Entities.Dynatrace
{
    public class V1Beta1DynaKubeTests
    {
        [Fact]
        public void When_V1Beta1DynaKube_is_deserialized_then_returned_properties_should_be_an_empty_type()
        {
            // JsonElement appears to break the compare function.

            const string json = @"{
              ""oneAgent"": {
                ""hostMonitoring"": {
                  ""tolerations"": [
                    {
                      ""effect"": ""NoSchedule"",
                      ""key"": ""node-role.kubernetes.io/master"",
                      ""operator"": ""Exists""
                    }
                  ]
                }
              }
            }";

            // Act
            var result = JsonSerializer.Deserialize<V1Beta1DynaKube.DynaKubeSpec>(json);

            // Assert
            result!.OneAgent!.HostMonitoring.Should().BeOfType<OneAgentSpec.EmptyObject>();
            result.OneAgent!.HostMonitoring.Should().NotBeOfType<JsonElement>();
        }
    }
}
