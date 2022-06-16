// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.Kube;
using Contrast.K8s.AgentOperator.Core.State.Resources;
using Contrast.K8s.AgentOperator.Core.State.Resources.Primitives;
using Contrast.K8s.AgentOperator.Entities.OpenShift;
using JetBrains.Annotations;
using k8s.Models;
using MediatR;

namespace Contrast.K8s.AgentOperator.Core.State.Appliers
{
    [UsedImplicitly]
    public class DeploymentConfigApplier : BaseApplier<V1DeploymentConfig, DeploymentConfigResource>
    {
        public DeploymentConfigApplier(IStateContainer stateContainer, IMediator mediator) : base(stateContainer, mediator)
        {
        }

        public override ValueTask<DeploymentConfigResource> CreateFrom(V1DeploymentConfig entity, CancellationToken cancellationToken = default)
        {
            var resource = new DeploymentConfigResource(
                entity.Uid(),
                entity.Metadata.GetLabels(),
                entity.Spec.Template.GetPod(),
                GetSelector(entity)
            );

            return ValueTask.FromResult(resource);
        }

        private static PodSelector GetSelector(V1DeploymentConfig entity)
        {
            var expressions = new List<PodMatchExpression>();
            if (entity.Spec.Selector is { } matchLabels)
            {
                foreach (var matchLabel in matchLabels)
                {
                    expressions.Add(new PodMatchExpression(matchLabel.Key, LabelMatchOperation.In, new List<string>
                    {
                        matchLabel.Value
                    }));
                }
            }

            var selector = new PodSelector(expressions);
            return selector;
        }
    }
}
