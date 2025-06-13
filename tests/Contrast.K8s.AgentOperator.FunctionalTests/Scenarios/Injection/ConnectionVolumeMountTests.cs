// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.FunctionalTests.Fixtures;
using FluentAssertions;
using FluentAssertions.Execution;
using k8s.Models;
using Xunit;
using Xunit.Abstractions;

namespace Contrast.K8s.AgentOperator.FunctionalTests.Scenarios.Injection;

public class ConnectionVolumeMountTests : IClassFixture<TestingContext>
{
    private const string ScenarioName = "connection-volume-mount";

    private readonly TestingContext _context;

    public ConnectionVolumeMountTests(TestingContext context, ITestOutputHelper outputHelper)
    {
        _context = context;
        _context.RegisterOutput(outputHelper);
    }

    [Fact]
    public async Task Pod_has_volume_and_config_env_var()
    {
        var client = await _context.GetClient();

        // Act
        var result = await client.GetInjectedPodByPrefix(ScenarioName);

        // Assert
        using (new AssertionScope())
        {
            result.Spec.Volumes.Should().ContainSingle(x => x.Name == "contrast-connection");
            var container = result.Spec.Containers.Single();
            var volumeMount = container.VolumeMounts.Should().ContainSingle(x => x.Name == "contrast-connection").Subject;
            volumeMount.MountPath.Should().Be("/contrast/connection");
            container.Env.Should().Contain(x => x.Name == "CONTRAST_CONFIG_PATH").Which.Value.Should().Be("/contrast/connection/contrast_security.yaml");
            container.Env.Should().ContainSingle(x => x.Name == "CONTRAST__API__URL");
        }
    }

    [Fact]
    public async Task Has_generated_secret()
    {
        var client = await _context.GetClient();

        // Act
        var result = await client.GetByPrefix<V1Secret>("agent-connection-volume-secret");

        // Assert
        result.Data.Should().ContainSingle(x => x.Key == "contrast_security.yaml");
    }
}
