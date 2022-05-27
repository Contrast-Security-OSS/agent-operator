using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Options;
using DotnetKubernetesClient;
using JsonDiffPatch;
using k8s;
using k8s.Autorest;
using k8s.Models;
using Newtonsoft.Json;
using NLog;

namespace Contrast.K8s.AgentOperator.Core.Kube
{
    public interface IResourcePatcher
    {
        Task<bool> Patch<T>(string name, string? @namespace, Action<T> mutator) where T : class, IKubernetesObject<V1ObjectMeta>;
    }

    public class ResourcePatcher : IResourcePatcher
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly JsonDiffer _jsonDiffer;
        private readonly IKubernetesClient _client;
        private readonly KubernetesJsonSerializer _jsonSerializer;
        private readonly OperatorOptions _operatorOptions;

        public ResourcePatcher(JsonDiffer jsonDiffer, IKubernetesClient client, KubernetesJsonSerializer jsonSerializer, OperatorOptions operatorOptions)
        {
            _jsonDiffer = jsonDiffer;
            _client = client;
            _jsonSerializer = jsonSerializer;
            _operatorOptions = operatorOptions;
        }

        public async Task<bool> Patch<T>(string name, string? @namespace, Action<T> mutator) where T : class, IKubernetesObject<V1ObjectMeta>
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Value must not be empty or null.", nameof(name));
            }

            if (@namespace != null && string.IsNullOrEmpty(@namespace))
            {
                throw new ArgumentException("If set, value cannot be empty.", nameof(@namespace));
            }

            var entity = await _client.Get<T>(name, @namespace);
            if (entity != null)
            {
                await Patch(entity, mutator);
            }
            else
            {
                Logger.Trace($"Could not locate entity '{entity.Namespace()}/{entity.Name()}' to patch.");
            }

            return entity != null;
        }

        private async Task Patch<T>(T entity, Action<T> mutator) where T : IKubernetesObject<V1ObjectMeta>
        {
            var stopwatch = Stopwatch.StartNew();
            var entityCopy = _jsonSerializer.DeepClone(entity);

            var currentVersion = _jsonSerializer.ToJToken(entityCopy);
            mutator.Invoke(entityCopy);
            var nextVersion = _jsonSerializer.ToJToken(entityCopy);

            var diff = _jsonDiffer.Diff(currentVersion, nextVersion, false);
            if (diff.Operations.Any())
            {
                Logger.Trace(
                    $"Peparing to patch '{entity.Namespace()}/{entity.Name()}' ('{entity.Kind}/{entity.ApiVersion}') with '{diff.ToString(Formatting.None)}'.");

                try
                {
                    await _client.Patch(entity, diff, _operatorOptions.FieldManagerName);
                }
                catch (HttpOperationException e) when (e.Response.StatusCode == HttpStatusCode.NotFound)
                {
                    Logger.Trace("Entity disappeared while patching.");
                }

                Logger.Trace($"Patch complete after {stopwatch.ElapsedMilliseconds}ms.");
            }
        }
    }
}
