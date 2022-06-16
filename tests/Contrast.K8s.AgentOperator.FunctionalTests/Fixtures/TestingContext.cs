// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using DotnetKubernetesClient;
using k8s;
using Xunit.Abstractions;

namespace Contrast.K8s.AgentOperator.FunctionalTests.Fixtures
{
    public class TestingContext : IDisposable
    {
        private readonly Lazy<IKubernetesClient> _lazy;
        private ITestOutputHelper? _outputHelper;

        private IKubernetesClient Client => _lazy.Value;

        public TestingContext()
        {
            _lazy = new Lazy<IKubernetesClient>(ClientFactory);
        }

        public void RegisterOutput(ITestOutputHelper outputHelper) => _outputHelper = outputHelper;

        public async Task<TestingClient> GetClient(string defaultNamespace = "testing")
        {
            var result = await Client.GetServerVersion();
            _outputHelper?.WriteLine($"Working with K8s version {result.GitVersion}.");

            return new TestingClient(Client, _outputHelper, new TestingClientOptions(defaultNamespace, TimeSpan.FromMinutes(5)));
        }

        private IKubernetesClient ClientFactory()
        {
            return new KubernetesClient(KubernetesClientConfiguration.BuildDefaultConfig());
        }

        public void Dispose()
        {
            if (_lazy.IsValueCreated)
            {
                _lazy.Value.ApiClient.Dispose();
            }
        }
    }
}
