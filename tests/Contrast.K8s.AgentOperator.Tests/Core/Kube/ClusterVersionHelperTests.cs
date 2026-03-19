// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.Kube;
using FluentAssertions;
using k8s;
using k8s.Autorest;
using k8s.Models;
using NSubstitute;
using Xunit;

namespace Contrast.K8s.AgentOperator.Tests.Core.Kube;


public class ClusterVersionHelperTests
{
    public static IEnumerable<object?[]> VersionData =>
        new List<object?[]>
        {
            new object?[] {"1", "35", new Version(1, 35) },
            new object?[] {"1", "34", new Version(1, 34) },
            new object?[] {"1", "35+", new Version(1, 35) },
            new object?[] {"1", "28+", new Version(1, 28) },
            new object?[] {"2", "0", new Version(2, 0) },
            new object?[] {null, "35", null },
            new object?[] {"1", null, null },
            new object?[] {"", "35", null },
            new object?[] {"1", "", null },
            new object?[] {"abc", "35", null },
            new object?[] {"1", "abc", null },

        };

    [Theory]
    [MemberData(nameof(VersionData))]
    public void TryParseClusterVersion_should_parse_correctly(string? major, string? minor, Version? expectedResult)
    {
        var versionInfo = new VersionInfo { Major = major, Minor = minor };
        var result = ClusterVersionHelper.TryParseClusterVersion(versionInfo);

        result.Should().Be(expectedResult);
    }

    [Fact]
    public void GetClusterVersion_should_return_valid()
    {
        var clusterVersion = new Version(1, 35);
        var versionInfo = new VersionInfo { Major = "1", Minor = "35" };
        var response = new HttpOperationResponse<VersionInfo> { Body = versionInfo };

        var versionOps = Substitute.For<IVersionOperations>();
        versionOps.GetCodeWithHttpMessagesAsync(
            Arg.Any<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
            Arg.Any<CancellationToken>()
        ).Returns(Task.FromResult(response));

        var client = Substitute.For<IKubernetes>();
        client.Version.Returns(versionOps);

        var result = ClusterVersionHelper.GetClusterVersion(client);

        result.Should().Be(clusterVersion);
    }
}
