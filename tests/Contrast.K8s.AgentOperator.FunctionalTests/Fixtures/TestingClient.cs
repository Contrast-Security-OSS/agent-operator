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

                var result = results.SingleOrDefault(x => Regex.IsMatch(x.Name(), @"^(" + normalizedPrefix + @")-[a-z0-9\-]+$"));
                if (result != null)
                {
                    return result;
                }

                _outputHelper?.WriteLine($"Entity {@namespace}/{name}-* ({typeof(T)}) does not exist, will try again in {_pollInterval.TotalSeconds} seconds.");
                await Task.Delay(_pollInterval, source.Token);
            }

            throw new TaskCanceledException();
        }

        public async Task<V1Pod> GetInjectedPodByPrefix(string name, string? @namespace = default)
        {
            const string convergedType = "agents.contrastsecurity.com/injection-converged";

            using var source = new CancellationTokenSource(_options.WaitDuration);
            while (!source.IsCancellationRequested)
            {
                var pod = await GetByPrefix<V1Pod>(name, @namespace);
                if (pod.Status.Conditions.FirstOrDefault(x => x.Type == convergedType) is { } condition)
                {
                    if (string.Equals(condition.Status, "True", StringComparison.OrdinalIgnoreCase))
                    {
                        return pod;
                    }

                    _outputHelper?.WriteLine($"Entity {@namespace}/{name}-* ({typeof(V1Pod)}) exists, but {convergedType}={condition.Status} "
                                             + $"(Reason: '{condition.Reason}', Message: '{condition.Message}', LastTransitionTime: '{condition.LastTransitionTime}'). "
                                             + $"Will try again in {_pollInterval.TotalSeconds} seconds.");
                }
                else
                {
                    _outputHelper?.WriteLine(
                        $"Entity {@namespace}/{name}-* ({typeof(V1Pod)}) exists, but has no {convergedType} condition. Will try again in {_pollInterval.TotalSeconds} seconds.");
                }

                await Task.Delay(_pollInterval, source.Token);
            }

            throw new TaskCanceledException();
        }
    }

    public record TestingClientOptions(string DefaultNamespace, TimeSpan WaitDuration);
}
