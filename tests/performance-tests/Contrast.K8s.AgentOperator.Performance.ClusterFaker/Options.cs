// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using CommandLine;

namespace Contrast.K8s.AgentOperator.Performance.ClusterFaker
{
    public static class Options
    {
        [Verb("up")]
        public class UpOptions
        {
            [Option('n', "namespaces")]
            public int NamespaceCount { get; set; } = 1;

            [Option('d', "deployments")]
            public int DeploymentsPerNamespaceCount { get; set; } = 1;

            [Option('p', "pods")]
            public int PodsPerDeploymentCount { get; set; } = 1;

            public override string ToString()
            {
                return
                    $"{nameof(NamespaceCount)}: {NamespaceCount}, {nameof(DeploymentsPerNamespaceCount)}: {DeploymentsPerNamespaceCount}, {nameof(PodsPerDeploymentCount)}: {PodsPerDeploymentCount}";
            }
        }

        [Verb("down")]
        public class DownOptions
        {
            public override string ToString()
            {
                return "";
            }
        }
    }
}
