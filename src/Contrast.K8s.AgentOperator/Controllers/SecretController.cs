// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Contrast.K8s.AgentOperator.Core.Kube;
using Contrast.K8s.AgentOperator.Core.State;
using JetBrains.Annotations;
using k8s.Models;
using KubeOps.Operator.Rbac;

namespace Contrast.K8s.AgentOperator.Controllers;

[UsedImplicitly]
// TODO Limit this is only my namespace?
[EntityRbac(typeof(V1Secret), Verbs = VerbConstants.AllButDelete)]
[EntityRbac(typeof(V1MutatingWebhookConfiguration), Verbs = VerbConstants.ReadAndPatch)]
public class SecretController : GenericController<V1Secret>
{
    public SecretController(IEventStream eventStream) : base(eventStream)
    {
    }
}
