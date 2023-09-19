// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Contrast.K8s.AgentOperator.Core.Kube;
using Contrast.K8s.AgentOperator.Core.State;
using Contrast.K8s.AgentOperator.Entities;
using JetBrains.Annotations;
using k8s.Models;
using KubeOps.Operator.Controller;
using KubeOps.Operator.Rbac;
using NLog;

namespace Contrast.K8s.AgentOperator.Controllers
{
    //[EntityRbac(typeof(V1alpha1Rollout), Verbs = VerbConstants.ReadAndPatch), UsedImplicitly]
    public class RolloutController : GenericController<V1alpha1Rollout> //IResourceController<V1alpha1Rollout>
    {
        //private readonly IEventStream _eventStream;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        public RolloutController(IEventStream eventStream) : base(eventStream)
        {
            //_eventStream = eventStream;
            Logger.Info($"RolloutController {eventStream.ToString()}");
        }
    }
}
