using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.State.Resources;
using Contrast.K8s.AgentOperator.Entities;
using JetBrains.Annotations;
using k8s.Models;
using MediatR;
using NLog;

namespace Contrast.K8s.AgentOperator.Core.State.Appliers
{
    [UsedImplicitly]
    public class AgentConfigurationApplier : BaseApplier<V1Beta1AgentConfiguration, AgentConfigurationResource>
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly IYamlParser _yamlParser;

        public AgentConfigurationApplier(IStateContainer stateContainer, IMediator mediator, IYamlParser yamlParser) : base(stateContainer, mediator)
        {
            _yamlParser = yamlParser;
        }

        public override ValueTask<AgentConfigurationResource> CreateFrom(V1Beta1AgentConfiguration entity, CancellationToken cancellationToken = default)
        {
            var yamlValues = new Dictionary<string, string>();
            if (entity.Spec.Yaml is { } yaml
                && !string.IsNullOrWhiteSpace(yaml))
            {
                var parsedYaml = _yamlParser.Parse(yaml, out var result);
                if (!result.IsValid)
                {
                    Logger.Error(
                        $"Failed to parse yaml in AgentConfiguration '{entity.Namespace()}/{entity.Name()}' and will be ignored (Error: '{result.Error}').");
                }
                else
                {
                    foreach (var (key, value) in parsedYaml)
                    {
                        if (value.Value != null)
                        {
                            yamlValues.Add(key, value.Value);
                        }
                    }
                }
            }

            var resource = new AgentConfigurationResource(yamlValues);
            return ValueTask.FromResult(resource);
        }
    }
}
