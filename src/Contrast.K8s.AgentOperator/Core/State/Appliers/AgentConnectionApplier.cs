using System;
using System.Threading;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.State.Resources;
using Contrast.K8s.AgentOperator.Core.State.Resources.Primitives;
using Contrast.K8s.AgentOperator.Entities;
using JetBrains.Annotations;
using k8s.Models;
using MediatR;
using NLog;

namespace Contrast.K8s.AgentOperator.Core.State.Appliers
{
    [UsedImplicitly]
    public class AgentConnectionApplier : BaseApplier<V1Beta1AgentConnection, AgentConnectionResource>
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public AgentConnectionApplier(IStateContainer stateContainer, IMediator mediator) : base(stateContainer, mediator)
        {
        }

        public override ValueTask<AgentConnectionResource> CreateFrom(V1Beta1AgentConnection entity, CancellationToken cancellationToken = default)
        {
            var spec = entity.Spec;

            var uri = new Uri("https://app.contrastsecurity.com/Contrast");
            if (spec.Url != null)
            {
                if (Uri.TryCreate(spec.Url, UriKind.Absolute, out var parsedUri))
                {
                    uri = parsedUri;
                }
                else
                {
                    Logger.Error($"Failed to parse the 'spec.url' '{spec.Url}', a default value of '{uri}' will be used.");
                }
            }

            var @namespace = entity.Namespace()!;

            var resource = new AgentConnectionResource(
                uri,
                new SecretReference(@namespace, spec.ApiKey.SecretName, spec.ApiKey.SecretKey),
                new SecretReference(@namespace, spec.ServiceKey.SecretName, spec.ServiceKey.SecretKey),
                new SecretReference(@namespace, spec.UserName.SecretName, spec.UserName.SecretKey)
            );
            return ValueTask.FromResult(resource);
        }
    }
}
