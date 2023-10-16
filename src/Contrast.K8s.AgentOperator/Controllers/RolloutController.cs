// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Contrast.K8s.AgentOperator.Core.Kube;
using Contrast.K8s.AgentOperator.Core.State;
using Contrast.K8s.AgentOperator.Entities.Argo;
using JetBrains.Annotations;
using KubeOps.Operator.Rbac;

namespace Contrast.K8s.AgentOperator.Controllers
{
    [EntityRbac(typeof(V1Alpha1Rollout), Verbs = VerbConstants.ReadAndPatch), UsedImplicitly]
    public class RolloutController : GenericController<V1Alpha1Rollout>
    {
        public RolloutController(IEventStream eventStream) : base(eventStream)
        {
        }
    }
}
