// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Contrast.K8s.AgentOperator.Core.Kube;
using Contrast.K8s.AgentOperator.Core.State.Resources;
using JetBrains.Annotations;
using k8s.Models;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Contrast.K8s.AgentOperator.Core.State.Appliers;

[UsedImplicitly]
public class NamespaceApplier : BaseApplier<V1Namespace, NamespaceResource>
{
    public NamespaceApplier(IStateContainer stateContainer, IMediator mediator) : base(stateContainer, mediator)
    {
    }

    public override ValueTask<NamespaceResource> CreateFrom(V1Namespace entity, CancellationToken cancellationToken = default)
    {
        var resource = new NamespaceResource(
            entity.Uid(),
            entity.Metadata.GetLabels()
        );
        return ValueTask.FromResult(resource);
    }

    protected override string GetNamespace(V1Namespace entity)
    {
        return entity.Name(); //Namespace() is null for namespaces so we use Name() TODO: make SateContainer hold namespaces separately 
    }

}

