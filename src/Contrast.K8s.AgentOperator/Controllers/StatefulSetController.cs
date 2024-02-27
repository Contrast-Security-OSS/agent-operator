// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Contrast.K8s.AgentOperator.Core.Kube;
using Contrast.K8s.AgentOperator.Core.State;
using JetBrains.Annotations;
using k8s.Models;
using KubeOps.Operator.Rbac;

namespace Contrast.K8s.AgentOperator.Controllers;

[EntityRbac(typeof(V1StatefulSet), Verbs = VerbConstants.ReadAndPatch), UsedImplicitly]
public class StatefulSetController : GenericController<V1StatefulSet>
{
    public StatefulSetController(IEventStream eventStream) : base(eventStream)
    {
    }
}
