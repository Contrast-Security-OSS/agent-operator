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
        public AgentInjectorApplier(IStateContainer stateContainer, IMediator mediator) : base(stateContainer, mediator)
        {
        }

        protected override async ValueTask<AgentInjectorResource> CreateFrom(V1Beta1AgentInjector entity, CancellationToken cancellationToken = default)
        {
            var spec = entity.Spec;
            var @namespace = entity.Namespace()!;

            var type = spec.Type switch
            {
                "dotnet-core" => AgentInjectionType.DotNetCore,
                _ => throw new ArgumentOutOfRangeException()
            };
            var image = await CalculateImage(spec, cancellationToken);
            var selector = GetSelector(spec, @namespace);
            var connectionReference = new AgentInjectorConnectionReference(@namespace, spec.Connection.Name);

            var configurationReference = spec.Configuration?.Name != null
                ? new AgentConfigurationReference(@namespace, spec.Configuration.Name)
                : null;

            var resource = new AgentInjectorResource(
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

        private static ValueTask<ContainerImageReference> CalculateImage(V1Beta1AgentInjector.AgentInjectorSpec spec,
                                                                         CancellationToken cancellationToken = default)
        {
            // TODO Validation, reach out and get images based on version, etc.
            // TODO Need defaults.
            // TODO Regex validation of image and repository.

            var version = spec.Version ?? "latest";
            if (version != "latest")
            {
                throw new NotImplementedException("Only latest version is currently supported.");
            }

            var image = new ContainerImageReference(
                spec.Image.Repository ?? throw new NotImplementedException("Repository has no defaults."),
                spec.Image.Name ?? throw new NotImplementedException("Name has no defaults."),
                version
            );

            return ValueTask.FromResult(image);
        }
    }
}
