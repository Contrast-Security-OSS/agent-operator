// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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

namespace Contrast.K8s.AgentOperator.Core.State.Appliers;

[UsedImplicitly]
public class AgentInjectorApplier : BaseApplier<V1Beta1AgentInjector, AgentInjectorResource>
{
    private readonly IImageGenerator _imageGenerator;

    public AgentInjectorApplier(IStateContainer stateContainer,
                                IMediator mediator,
                                IImageGenerator imageGenerator) : base(stateContainer, mediator)
    {
        _imageGenerator = imageGenerator;
    }

    public override async ValueTask<AgentInjectorResource> CreateFrom(V1Beta1AgentInjector entity, CancellationToken cancellationToken = default)
    {
        var spec = entity.Spec;
        var @namespace = entity.Namespace()!;

        var enabled = spec.Enabled;
        var type = AgentInjectionTypeConverter.GetTypeFromString(spec.Type);
        var imageReference = await _imageGenerator.GenerateImage(type,
            spec.Image.Registry,
            spec.Image.Name,
            spec.Version,
            cancellationToken
        );
        var selector = GetSelector(spec, @namespace);

        var namespaceDefaultConnectionName = ClusterDefaults.AgentConnectionName(@namespace);
        var connectionName = spec.Connection?.Name ?? namespaceDefaultConnectionName;
        var isConnectionNamespaceDefault = connectionName == namespaceDefaultConnectionName;
        var connectionReference = new AgentInjectorConnectionReference(@namespace, connectionName, isConnectionNamespaceDefault);

        var namespaceDefaultConfigurationName = ClusterDefaults.AgentConfigurationName(@namespace);
        var configurationName = spec.Configuration?.Name ?? namespaceDefaultConfigurationName;
        var isConfigurationNamespaceDefault = namespaceDefaultConfigurationName == configurationName;
        var configurationReference = new AgentConfigurationReference(@namespace, configurationName, isConfigurationNamespaceDefault);

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

        var labels = spec.Selector.Labels.Select(x => new LabelPattern(x.Name, x.Value)).ToList();

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
