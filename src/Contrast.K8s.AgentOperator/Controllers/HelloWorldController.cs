using System;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core;
using k8s.Models;
using KubeOps.Operator.Controller;
using KubeOps.Operator.Controller.Results;
using KubeOps.Operator.Rbac;
using NLog;

namespace Contrast.K8s.AgentOperator.Controllers
{
    [EntityRbac(typeof(V1Deployment), Verbs = VerbConstants.ReadAndPatch)]
    public class HelloWorldController : IResourceController<V1Deployment>
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly IResourcePatcher _patcher;

        public HelloWorldController(IResourcePatcher patcher)
        {
            _patcher = patcher;
        }

        public async Task<ResourceControllerResult?> ReconcileAsync(V1Deployment entity)
        {
            Logger.Info($"Deployment '{entity.Name()}' reconciling.");

            if (string.Equals(entity.GetLabel("operatorSafe"), "true", StringComparison.OrdinalIgnoreCase))
            {
                await _patcher.Patch(entity, x => { x.Metadata.Labels["test"] = "true"; });
            }

            return null;
        }

        public Task DeletedAsync(V1Deployment entity)
        {
            Logger.Info($"Deployment '{entity.Name()}' deleted.");

            return Task.CompletedTask;
        }
    }
}
