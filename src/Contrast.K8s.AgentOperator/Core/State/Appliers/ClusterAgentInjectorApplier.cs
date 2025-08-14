// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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

namespace Contrast.K8s.AgentOperator.Core.State.Appliers;

[UsedImplicitly]
public class ClusterAgentInjectorApplier : BaseApplier<V1Beta1ClusterAgentInjector, ClusterAgentInjectorResource>
{
    private readonly IImageGenerator _imageGenerator;
    private readonly IAgentInjectionTypeConverter _typeConverter;

    public ClusterAgentInjectorApplier(IStateContainer stateContainer,
        IMediator mediator,
        IImageGenerator imageGenerator,
        IAgentInjectionTypeConverter typeConverter) : base(
        stateContainer, mediator)
    {
        _imageGenerator = imageGenerator;
        _typeConverter = typeConverter;
    }

    public override async ValueTask<ClusterAgentInjectorResource> CreateFrom(V1Beta1ClusterAgentInjector entity,
        CancellationToken cancellationToken = default)
    {
        var spec = entity.Spec.Template?.Spec!;

        var type = _typeConverter.GetTypeFromString(spec.Type);
        var imageReference = await _imageGenerator.GenerateImage(type,
            spec.Image.Registry,
            spec.Image.Name,
            spec.Version,
            cancellationToken
        );
        var selector = GetSelector(spec);

        var pullSecretName = spec.Image.PullSecretName != null
            ? new SecretReference(entity.Namespace(), spec.Image.PullSecretName, ".dockerconfigjson")
            : null;
        var pullPolicy = spec.Image.PullPolicy ?? "Always";

        var template = new AgentInjectorTemplate(
            spec.Enabled,
            type,
            imageReference,
            selector,
            pullSecretName,
            pullPolicy
        );
        var namespaces = entity.Spec.Namespaces;
        var namespaceLabels = entity.Spec.NamespaceLabelSelector.Select(x => new LabelPattern(x.Name, x.Value)).ToList();

        return new ClusterAgentInjectorResource(template, namespaces, namespaceLabels);
    }

    public static ResourceWithPodSpecSelector GetSelector(V1Beta1ClusterAgentInjector.AgentInjectorTemplateSpec spec)
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
            Array.Empty<string>()
        );
        return selector;
    }
}
