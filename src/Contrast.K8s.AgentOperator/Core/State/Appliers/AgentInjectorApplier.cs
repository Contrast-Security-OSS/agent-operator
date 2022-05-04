using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
        private readonly IImageGenerator _generator;

        public AgentInjectorApplier(IStateContainer stateContainer, IMediator mediator, IImageGenerator generator) : base(stateContainer, mediator)
        {
            _generator = generator;
        }

        protected override async ValueTask<AgentInjectorResource> CreateFrom(V1Beta1AgentInjector entity, CancellationToken cancellationToken = default)
        {
            var spec = entity.Spec;
            var @namespace = entity.Namespace()!;

            var enabled = spec.Enabled;
            var type = spec.Type switch
            {
                "dotnet-core" => AgentInjectionType.DotNetCore,
                "java" => AgentInjectionType.Java,
                _ => throw new ArgumentOutOfRangeException()
            };
            var image = await _generator.GenerateImage(type, spec.Image.Repository, spec.Image.Name, spec.Version, cancellationToken);
            var selector = GetSelector(spec, @namespace);
            var connectionReference = new AgentInjectorConnectionReference(@namespace, spec.Connection.Name);

            var configurationReference = spec.Configuration?.Name != null
                ? new AgentConfigurationReference(@namespace, spec.Configuration.Name)
                : null;

            var resource = new AgentInjectorResource(
                enabled,
                type,
                image,
                selector,
                connectionReference,
                configurationReference
            );
            return resource;
        }

        private static ResourceWithPodSpecSelector GetSelector(V1Beta1AgentInjector.AgentInjectorSpec spec, string @namespace)
        {
            var images = spec.Selector.Labels.Any()
                ? spec.Selector.Images
                : new List<string>
                {
                    "*"
                };

            var selector = new ResourceWithPodSpecSelector(
                images,
                spec.Selector.Labels,
                new List<string>
                {
                    @namespace
                }
            );
            return selector;
        }
    }
}
