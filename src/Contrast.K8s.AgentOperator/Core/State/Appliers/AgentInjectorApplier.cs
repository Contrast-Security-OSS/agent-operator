using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.Reactions.Defaults;
using Contrast.K8s.AgentOperator.Core.State.Resources;
using Contrast.K8s.AgentOperator.Core.State.Resources.Primitives;
using Contrast.K8s.AgentOperator.Entities;
using JetBrains.Annotations;
using k8s.Models;
using MediatR;

namespace Contrast.K8s.AgentOperator.Core.State.Appliers
{
    [UsedImplicitly]
    public class AgentInjectorApplier : BaseApplier<V1Beta1AgentInjector, AgentInjectorResource>
    {
        private readonly IImageGenerator _imageGenerator;
        private readonly IAgentInjectionTypeConverter _typeConverter;
        private readonly ClusterDefaults _clusterDefaults;

        public AgentInjectorApplier(IStateContainer stateContainer,
                                    IMediator mediator,
                                    IImageGenerator imageGenerator,
                                    IAgentInjectionTypeConverter typeConverter,
                                    ClusterDefaults clusterDefaults) : base(stateContainer, mediator)
        {
            _imageGenerator = imageGenerator;
            _typeConverter = typeConverter;
            _clusterDefaults = clusterDefaults;
        }

        public override async ValueTask<AgentInjectorResource> CreateFrom(V1Beta1AgentInjector entity, CancellationToken cancellationToken = default)
        {
            var spec = entity.Spec;
            var @namespace = entity.Namespace()!;

            var enabled = spec.Enabled;
            var type = _typeConverter.GetTypeFromString(spec.Type);
            var imageReference = await _imageGenerator.GenerateImage(type,
                spec.Image.Registry,
                spec.Image.Name,
                spec.Version,
                cancellationToken
            );
            var selector = GetSelector(spec, @namespace);

            var connectionName = spec.Connection?.Name ?? _clusterDefaults.GetDefaultAgentConnectionName(@namespace);
            var connectionReference = new AgentInjectorConnectionReference(@namespace, connectionName);

            var configurationName = spec.Configuration?.Name ?? _clusterDefaults.GetDefaultAgentConfigurationName(@namespace);
            var configurationReference = new AgentConfigurationReference(@namespace, configurationName);

            var pullSecretName = spec.Image.PullSecretName != null ? new SecretReference(@namespace, spec.Image.PullSecretName, ".dockerconfigjson") : null;
            var pullPolicy = spec.Image.PullPolicy ?? "Always";

            var resource = new AgentInjectorResource(
                enabled,
                type,
                imageReference,
                selector,
                connectionReference,
                configurationReference,
                pullSecretName,
                pullPolicy
            );
            return resource;
        }

        private static ResourceWithPodSpecSelector GetSelector(V1Beta1AgentInjector.AgentInjectorSpec spec, string @namespace)
        {
            var images = spec.Selector.Images.Any()
                ? spec.Selector.Images
                : new List<string>
                {
                    "*"
                };

            var labels = spec.Selector.Labels.Select(x => new KeyValuePair<string, string>(x.Name, x.Value)).ToList();

            var selector = new ResourceWithPodSpecSelector(
                images,
                labels,
                new List<string>
                {
                    @namespace
                }
            );
            return selector;
        }
    }
}
