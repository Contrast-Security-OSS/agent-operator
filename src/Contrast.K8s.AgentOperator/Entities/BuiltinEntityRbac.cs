// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Contrast.K8s.AgentOperator.Core.Kube;
using JetBrains.Annotations;
using k8s.Models;
using KubeOps.Abstractions.Rbac;

namespace Contrast.K8s.AgentOperator.Entities
{
    //Dummy entity so rbac for builtin models generates correctly
    [EntityRbac(typeof(V1DaemonSet), Verbs = VerbConstants.ReadAndPatch)]
    [EntityRbac(typeof(V1Deployment), Verbs = VerbConstants.ReadAndPatch)]
    [EntityRbac(typeof(V1Pod), Verbs = VerbConstants.ReadAndPatch)]
    [EntityRbac(typeof(V1Secret), Verbs = VerbConstants.All)]
    [EntityRbac(typeof(V1MutatingWebhookConfiguration), Verbs = VerbConstants.ReadAndPatch)]
    [EntityRbac(typeof(V1StatefulSet), Verbs = VerbConstants.ReadAndPatch)]
    [UsedImplicitly]
    public class BuiltinEntityRbac
    {
    }
}
