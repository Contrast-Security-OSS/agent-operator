// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.Events;
using Contrast.K8s.AgentOperator.Entities.Dynatrace;
using Contrast.K8s.AgentOperator.Options;
using JetBrains.Annotations;
using MediatR;
using NLog;

namespace Contrast.K8s.AgentOperator.Core.Chaining;

[UsedImplicitly]
public class DynaKubeHandler : INotificationHandler<EntityReconciled<V1Beta1DynaKube>>
{
    private readonly InjectorOptions _injectorOptions;
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public DynaKubeHandler(InjectorOptions injectorOptions)
    {
        _injectorOptions = injectorOptions;
    }

    public Task Handle(EntityReconciled<V1Beta1DynaKube> notification, CancellationToken cancellationToken)
    {
        if (!_injectorOptions.EnableEarlyChaining)
        {
            var oneAgentSpec = notification.Entity.Spec.OneAgent;
            if (oneAgentSpec?.ClassicFullStack != null)
            {
                Logger.Warn("Dynatrace Operator is present and in classicFullStack mode. "
                            + "If you are using a 'dotnet-core' AgentInjector, please set the environment variable 'CONTRAST_ENABLE_EARLY_CHAINING=true' on the agent-operator and restart the affected pods.");
            }
        }

        return Task.CompletedTask;
    }
}
