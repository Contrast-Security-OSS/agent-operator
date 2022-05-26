using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DotnetKubernetesClient;
using k8s;
using k8s.Models;
using Xunit.Abstractions;

namespace Contrast.K8s.AgentOperator.FunctionalTests.Fixtures
{
    public class TestingClient
    {
        private readonly IKubernetesClient _client;
        private readonly ITestOutputHelper? _outputHelper;
        private readonly TestingClientOptions _options;

        private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(1);

        public TestingClient(IKubernetesClient client, ITestOutputHelper? outputHelper, TestingClientOptions options)
        {
            _client = client;
            _outputHelper = outputHelper;
            _options = options;
        }

        public async Task<T> Get<T>(string name, string? @namespace = default) where T : class, IKubernetesObject<V1ObjectMeta>
        {
            @namespace ??= _options.DefaultNamespace;

            using var source = new CancellationTokenSource(_options.WaitDuration);
            while (!source.IsCancellationRequested)
            {
                var result = await _client.Get<T>(name, @namespace);
                if (result != null)
                {
                    return result;
                }

                _outputHelper?.WriteLine($"Entity {@namespace}/{name} ({typeof(T)}) does not exist, will try again in {_pollInterval.TotalSeconds} seconds.");
                await Task.Delay(_pollInterval, source.Token);
            }

            throw new TaskCanceledException();
        }

        public async Task<T> GetByPrefix<T>(string name, string? @namespace = default) where T : class, IKubernetesObject<V1ObjectMeta>
        {
            @namespace ??= _options.DefaultNamespace;

            using var source = new CancellationTokenSource(_options.WaitDuration);
            while (!source.IsCancellationRequested)
            {
                var results = await _client.List<T>(@namespace);
                var normalizedPrefix = Regex.Escape(name);

                var result = results.SingleOrDefault(x => Regex.IsMatch(x.Name(), @"^(" + normalizedPrefix + @")-[a-z0-9]{8,10}-[a-z0-9]{5}$"));
                if (result != null)
                {
                    return result;
                }

                _outputHelper?.WriteLine($"Entity {@namespace}/{name}-* ({typeof(T)}) does not exist, will try again in {_pollInterval.TotalSeconds} seconds.");
                await Task.Delay(_pollInterval, source.Token);
            }

            throw new TaskCanceledException();
        }
    }

    public record TestingClientOptions(string DefaultNamespace, TimeSpan WaitDuration);
}
