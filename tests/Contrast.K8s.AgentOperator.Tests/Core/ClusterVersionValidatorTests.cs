// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using Contrast.K8s.AgentOperator.Core;
using Contrast.K8s.AgentOperator.Options;
using FluentAssertions;
using FluentAssertions.Execution;
using k8s;
using k8s.Autorest;
using k8s.Models;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Contrast.K8s.AgentOperator.Tests.Core;

public class ClusterVersionValidatorTests
{
    private static readonly Fixture AutoFixture = new();

    // --- TryParseClusterVersion ---

    [Theory]
    [InlineData("1", "35", true, 1, 35)]
    [InlineData("1", "34", true, 1, 34)]
    [InlineData("1", "35+", true, 1, 35)]
    [InlineData("1", "28+", true, 1, 28)]
    [InlineData("2", "0", true, 2, 0)]
    [InlineData(null, "35", false, 0, 0)]
    [InlineData("1", null, false, 0, 0)]
    [InlineData("", "35", false, 0, 0)]
    [InlineData("1", "", false, 0, 0)]
    [InlineData("abc", "35", false, 0, 0)]
    [InlineData("1", "abc", false, 1, 0)]
    public void TryParseClusterVersion_should_parse_correctly(
        string? major, string? minor, bool expectedResult, int expectedMajor, int expectedMinor)
    {
        var result = ClusterVersionValidator.TryParseClusterVersion(major, minor, out var parsedMajor, out var parsedMinor);

        using (new AssertionScope())
        {
            result.Should().Be(expectedResult);
            parsedMajor.Should().Be(expectedMajor);
            parsedMinor.Should().Be(expectedMinor);
        }
    }

    // --- ValidateOptions behavior ---

    private static OperatorOptions CreateOptions(bool useImageVolumes) =>
        AutoFixture.Build<OperatorOptions>()
            .With(o => o.UseImageVolumes, useImageVolumes)
            .Create();

    private static IKubernetes CreateKubeClient(string? major = "1", string? minor = "35")
    {
        var versionInfo = new VersionInfo { Major = major, Minor = minor };
        var response = new HttpOperationResponse<VersionInfo> { Body = versionInfo };

        var versionOps = Substitute.For<IVersionOperations>();
        versionOps.GetCodeWithHttpMessagesAsync(
            Arg.Any<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
            Arg.Any<CancellationToken>()
        ).Returns(Task.FromResult(response));

        var client = Substitute.For<IKubernetes>();
        client.Version.Returns(versionOps);
        return client;
    }

    [Fact]
    public void When_image_volumes_disabled_should_return_unchanged()
    {
        var options = CreateOptions(useImageVolumes: false);
        var client = CreateKubeClient(major: "1", minor: "30");

        var result = ClusterVersionValidator.ValidateOptions(options, client);

        result.UseImageVolumes.Should().BeFalse();
    }

    [Fact]
    public void When_cluster_version_meets_requirement_should_keep_enabled()
    {
        var options = CreateOptions(useImageVolumes: true);
        var client = CreateKubeClient(major: "1", minor: "35");

        var result = ClusterVersionValidator.ValidateOptions(options, client);

        result.UseImageVolumes.Should().BeTrue();
    }

    [Fact]
    public void When_cluster_version_exceeds_requirement_should_keep_enabled()
    {
        var options = CreateOptions(useImageVolumes: true);
        var client = CreateKubeClient(major: "1", minor: "36");

        var result = ClusterVersionValidator.ValidateOptions(options, client);

        result.UseImageVolumes.Should().BeTrue();
    }

    [Fact]
    public void When_cluster_version_below_requirement_should_disable_option()
    {
        var options = CreateOptions(useImageVolumes: true);
        var client = CreateKubeClient(major: "1", minor: "34");

        var result = ClusterVersionValidator.ValidateOptions(options, client);

        result.UseImageVolumes.Should().BeFalse();
    }

    [Fact]
    public void When_cluster_version_has_trailing_plus_and_meets_requirement_should_keep_enabled()
    {
        var options = CreateOptions(useImageVolumes: true);
        var client = CreateKubeClient(major: "1", minor: "35+");

        var result = ClusterVersionValidator.ValidateOptions(options, client);

        result.UseImageVolumes.Should().BeTrue();
    }

    [Fact]
    public void When_cluster_version_unparseable_should_leave_enabled()
    {
        var options = CreateOptions(useImageVolumes: true);
        var client = CreateKubeClient(major: "abc", minor: "xyz");

        var result = ClusterVersionValidator.ValidateOptions(options, client);

        result.UseImageVolumes.Should().BeTrue();
    }

    [Fact]
    public void When_version_query_throws_should_return_unchanged()
    {
        var options = CreateOptions(useImageVolumes: true);

        var versionOps = Substitute.For<IVersionOperations>();
        versionOps.GetCodeWithHttpMessagesAsync(
            Arg.Any<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
            Arg.Any<CancellationToken>()
        ).ThrowsAsync(new Exception("Connection refused"));

        var client = Substitute.For<IKubernetes>();
        client.Version.Returns(versionOps);

        var result = ClusterVersionValidator.ValidateOptions(options, client);

        result.UseImageVolumes.Should().BeTrue();
    }

    [Fact]
    public void Should_preserve_other_options_when_disabling()
    {
        var options = AutoFixture.Build<OperatorOptions>()
            .With(o => o.UseImageVolumes, true)
            .Create();
        var client = CreateKubeClient(major: "1", minor: "34");

        var result = ClusterVersionValidator.ValidateOptions(options, client);

        using (new AssertionScope())
        {
            result.UseImageVolumes.Should().BeFalse();
            result.Namespace.Should().Be(options.Namespace);
            result.SettlingDurationSeconds.Should().Be(options.SettlingDurationSeconds);
            result.WatcherTimeoutSeconds.Should().Be(options.WatcherTimeoutSeconds);
            result.EventQueueSize.Should().Be(options.EventQueueSize);
            result.EventQueueFullMode.Should().Be(options.EventQueueFullMode);
            result.EventQueueMergeWindowSeconds.Should().Be(options.EventQueueMergeWindowSeconds);
            result.RunInitContainersAsNonRoot.Should().Be(options.RunInitContainersAsNonRoot);
            result.SuppressSeccompProfile.Should().Be(options.SuppressSeccompProfile);
            result.EnableAgentStdout.Should().Be(options.EnableAgentStdout);
            result.ChaosRatio.Should().Be(options.ChaosRatio);
        }
    }
}
