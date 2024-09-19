// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.FunctionalTests.Fixtures;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;
using Xunit.Abstractions;

namespace Contrast.K8s.AgentOperator.FunctionalTests.Scenarios.Injection;

public class YamlVariablesTests : IClassFixture<TestingContext>
{
    private const string ScenarioName = "yaml-variables";

    private readonly TestingContext _context;

    public YamlVariablesTests(TestingContext context, ITestOutputHelper outputHelper)
    {
        _context = context;
        _context.RegisterOutput(outputHelper);
    }

    [Fact]
    public async Task When_injected_then_pod_should_have_injection_annotations()
    {
        var client = await _context.GetClient(defaultNamespace: "testing-variables");

        // Act
        var result = await client.GetInjectedPodByPrefix(ScenarioName);

        // Assert
        using (new AssertionScope())
        {
            var container = result.Spec.Containers.Should().ContainSingle().Subject;

            //namespace
            var namespaceKey = container.Env.Should().Contain(x => x.Name == "CONTRAST_VAR_POD_NAMESPACE").Subject;
            namespaceKey.ValueFrom.FieldRef.Should().Be("metadata.namespace");
            container.Env.Should().Contain(x => x.Name == "CONTRAST__TEST__NAMESPACE")
                .Which.Value.Should().Be("$(CONTRAST_VAR_POD_NAMESPACE)");

            //label
            var labelKey = container.Env.Should().Contain(x => x.Name == "CONTRAST_VAR_LABEL_TESTLABEL").Subject;
            labelKey.ValueFrom.FieldRef.Should().Be("metadata.labels['test-label']");
            container.Env.Should().Contain(x => x.Name == "CONTRAST__TEST__LABEL")
                .Which.Value.Should().Be("$(CONTRAST_VAR_LABEL_TESTLABEL)");

            //annotation
            var annotationKey = container.Env.Should().Contain(x => x.Name == "CONTRAST_VAR_ANNOTATION_TESTANNOTATION").Subject;
            annotationKey.ValueFrom.FieldRef.Should().Be("metadata.labels['test-annotation']");
            container.Env.Should().Contain(x => x.Name == "CONTRAST__TEST__ANNOTATION")
                .Which.Value.Should().Be("$(CONTRAST_VAR_ANNOTATION_TESTANNOTATION)");

            //container image
            container.Env.Should().Contain(x => x.Name == "CONTRAST__TEST__CONTAINER_IMAGE")
                .Which.Value.Should().Be("dotnet-test");

            //multiple
            container.Env.Should().Contain(x => x.Name == "CONTRAST__TEST__MULTIPLE")
                .Which.Value.Should().Be("$(CONTRAST_VAR_POD_NAMESPACE)_$(CONTRAST_VAR_POD_NAMESPACE)");
        }

    }
}
