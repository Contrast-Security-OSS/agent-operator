// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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

namespace Contrast.K8s.AgentOperator.Core.State.Appliers;

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

        string uri = null!;
        if (spec.Token == null)
        {
            //Token is not set so set the default url (since the token could contain the URL and agents will prioritize the url env var over the one in the token )
            uri = "https://app-agents.contrastsecurity.com/Contrast";
        }

        if (spec.Url != null)
        {
            if (Uri.TryCreate(spec.Url, UriKind.Absolute, out _))
            {
                // Don't use the parsed version, or we can run into looping if the parsed version is normalized.
                uri = spec.Url;
            }
            else
            {
                Logger.Error($"Failed to parse the 'spec.url' '{spec.Url}', a default value of '{uri}' will be used.");
            }
        }

        var @namespace = entity.Namespace()!;

        SecretReference? token = null;
        if (spec.Token != null)
        {
            token = new SecretReference(@namespace, spec.Token.SecretName, spec.Token.SecretKey);
        }

        SecretReference? apiKey = null;
        if (spec.ApiKey != null)
        {
            apiKey = new SecretReference(@namespace, spec.ApiKey.SecretName, spec.ApiKey.SecretKey);
        }

        SecretReference? serviceKey = null;
        if (spec.ServiceKey != null)
        {
            serviceKey = new SecretReference(@namespace, spec.ServiceKey.SecretName, spec.ServiceKey.SecretKey);
        }

        SecretReference? username = null;
        if (spec.UserName != null)
        {
            username = new SecretReference(@namespace, spec.UserName.SecretName, spec.UserName.SecretKey);
        }

        var resource = new AgentConnectionResource(
            spec.MountAsVolume,
            token,
            uri,
            apiKey,
            serviceKey,
            username
        );
        return ValueTask.FromResult(resource);
    }
}
