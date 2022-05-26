using System.Linq;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.FunctionalTests.Fixtures;
using FluentAssertions;
using FluentAssertions.Execution;
using k8s.Models;
using Xunit;
using Xunit.Abstractions;

namespace Contrast.K8s.AgentOperator.FunctionalTests.Scenarios.Injection
{
    public class StandardInjectionTests : IClassFixture<TestingContext>
    {
        private const string ScenarioName = "injection-dummy";

        private readonly TestingContext _context;

        public StandardInjectionTests(TestingContext context, ITestOutputHelper outputHelper)
        {
            _context = context;
            _context.RegisterOutput(outputHelper);
        }

        [Fact]
        public async Task When_injected_then_pod_should_have_injection_annotations()
        {
            var client = await _context.GetClient();

            // Act
            var result = await client.GetByPrefix<V1Pod>(ScenarioName);

            // Assert
            using (new AssertionScope())
            {
                result.Annotations().Should().ContainKey("agents.contrastsecurity.com/injected-on");
                result.Annotations().Should().ContainKey("agents.contrastsecurity.com/injector-hash");
                result.Annotations().Should().ContainKey("agents.contrastsecurity.com/injector-name").WhoseValue.Should().Be(ScenarioName);
                result.Annotations().Should().ContainKey("agents.contrastsecurity.com/injector-namespace").WhoseValue.Should().Be("testing");
                result.Annotations().Should().ContainKey("agents.contrastsecurity.com/is-injected").WhoseValue.Should().Be("True");
            }
        }

        [Fact]
        public async Task When_injected_then_pod_should_have_standard_injection_environment_variables()
        {
            var client = await _context.GetClient();

            // Act
            var result = await client.GetByPrefix<V1Pod>(ScenarioName);

            // Assert
            using (new AssertionScope())
            {
                var container = result.Spec.Containers.Single();
                container.Env.Should().Contain(x => x.Name == "CONTRAST_MOUNT_PATH").Which.Value.Should().Be("/contrast");
                container.Env.Should().Contain(x => x.Name == "CONTRAST__API__URL").Which.Value.Should().Be("http://localhost/");
                container.Env.Should().Contain(x => x.Name == "CONTRAST_CLUSTER_ID").Which.Value.Should().NotBeNull();

                var apiKey = container.Env.Should().Contain(x => x.Name == "CONTRAST__API__API_KEY").Subject;
                apiKey.ValueFrom.SecretKeyRef.Name.Should().Be("testing-agent-connection-secret");
                apiKey.ValueFrom.SecretKeyRef.Key.Should().Be("apiKey");

                var serviceKey = container.Env.Should().Contain(x => x.Name == "CONTRAST__API__SERVICE_KEY").Subject;
                serviceKey.ValueFrom.SecretKeyRef.Name.Should().Be("testing-agent-connection-secret");
                serviceKey.ValueFrom.SecretKeyRef.Key.Should().Be("serviceKey");

                var userName = container.Env.Should().Contain(x => x.Name == "CONTRAST__API__USER_NAME").Subject;
                userName.ValueFrom.SecretKeyRef.Name.Should().Be("testing-agent-connection-secret");
                userName.ValueFrom.SecretKeyRef.Key.Should().Be("userName");
            }
        }

        [Fact]
        public async Task When_injected_then_pod_should_have_volume_mount()
        {
            var client = await _context.GetClient();

            // Act
            var result = await client.GetByPrefix<V1Pod>(ScenarioName);

            // Assert
            using (new AssertionScope())
            {
                var container = result.Spec.Containers.Single();
                var mount = container.VolumeMounts.Should().ContainSingle(x => x.Name == "contrast").Subject;
                mount.MountPath.Should().Be("/contrast");
                mount.ReadOnlyProperty.Should().BeTrue();
            }
        }

        [Fact]
        public async Task When_injected_then_pod_should_have_injection_common_config_environment_variables()
        {
            var client = await _context.GetClient();

            // Act
            var result = await client.GetByPrefix<V1Pod>(ScenarioName);

            // Assert
            using (new AssertionScope())
            {
                var container = result.Spec.Containers.Single();
                container.Env.Should().Contain(x => x.Name == "CONTRAST__ENABLED").Which.Value.Should().Be("false");
                container.Env.Should().Contain(x => x.Name == "CONTRAST__FOO__BAR").Which.Value.Should().Be("foobar");
            }
        }

        [Fact]
        public async Task When_injected_then_pod_should_have_injection_init_container()
        {
            var client = await _context.GetClient();

            // Act
            var result = await client.GetByPrefix<V1Pod>(ScenarioName);

            // Assert
            using (new AssertionScope())
            {
                var container = result.Spec.InitContainers.Single(x => x.Name == "contrast-init");
                container.ImagePullPolicy.Should().Be("Always");

                var mount = container.VolumeMounts.Should().ContainSingle().Subject;
                mount.Name.Should().Be("contrast");
                mount.MountPath.Should().Be("/contrast");
            }
        }

        [Fact]
        public async Task When_injected_then_pod_should_have_injection_volume()
        {
            var client = await _context.GetClient();

            // Act
            var result = await client.GetByPrefix<V1Pod>(ScenarioName);

            // Assert
            using (new AssertionScope())
            {
                result.Spec.Volumes.Should().ContainSingle(x => x.Name == "contrast")
                      .Which.EmptyDir.Should().NotBeNull();
            }
        }
    }
}
