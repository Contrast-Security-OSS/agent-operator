// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using CommandLine;
using DotnetKubernetesClient;
using static Contrast.K8s.AgentOperator.Performance.ClusterFaker.Options;

namespace Contrast.K8s.AgentOperator.Performance.ClusterFaker
{
    internal class Program
    {
        private static async Task<int> Main(string[] args)
        {
            var faker = new Faker(new KubernetesClient());
            return await Parser.Default.ParseArguments<UpOptions, DownOptions>(args)
                               .MapResult(
                                   (UpOptions opts) => faker.Up(opts),
                                   (DownOptions opts) => faker.Down(opts),
                                   errs => Task.FromResult(1));
        }
    }
}
