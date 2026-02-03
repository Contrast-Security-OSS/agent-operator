// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Contrast.K8s.AgentOperator.Core.State.Resources;
using Contrast.K8s.AgentOperator.Core.State.Resources.Primitives;
using Contrast.K8s.AgentOperator.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Contrast.K8s.AgentOperator.Core.Reactions.Defaults.Common;

public class ConnectionSyncing
{
    private readonly ISecretHelper _secretHelper;

    public ConnectionSyncing(ISecretHelper secretHelper)
    {
        _secretHelper = secretHelper;
    }

    public AgentConnectionResource CreateConnectionResource(
        AgentConnectionResource template,
        string secretName,
        string targetNamespace)
    {
        SecretReference? token = null;
        if (template.Token != null)
        {
            token = new SecretReference(targetNamespace, secretName, ClusterDefaults.DefaultTokenSecretKey);
        }

        SecretReference? apiKey = null;
        if (template.ApiKey != null)
        {
            apiKey = new SecretReference(targetNamespace, secretName, ClusterDefaults.DefaultApiKeySecretKey);
        }

        SecretReference? serviceKey = null;
        if (template.ServiceKey != null)
        {
            serviceKey = new SecretReference(targetNamespace, secretName, ClusterDefaults.DefaultServiceKeySecretKey);
        }

        SecretReference? username = null;
        if (template.UserName != null)
        {
            username = new SecretReference(targetNamespace, secretName, ClusterDefaults.DefaultUsernameSecretKey);
        }

        return template with
        {
            Token = token,
            ApiKey = apiKey,
            ServiceKey = serviceKey,
            UserName = username
        };
    }

    public V1Beta1AgentConnection.AgentConnectionSpec CreateConnectionSpec(AgentConnectionResource desiredResource)
    {
        var spec = new V1Beta1AgentConnection.AgentConnectionSpec
        {
            MountAsVolume = desiredResource.MountAsVolume,
            Url = desiredResource.TeamServerUri
        };

        if (desiredResource.Token != null)
        {
            spec.Token = new V1Beta1AgentConnection.SecretRef
            {
                SecretName = desiredResource.Token.Name,
                SecretKey = desiredResource.Token.Key
            };
        }

        if (desiredResource.ApiKey != null)
        {
            spec.ApiKey = new V1Beta1AgentConnection.SecretRef
            {
                SecretName = desiredResource.ApiKey.Name,
                SecretKey = desiredResource.ApiKey.Key
            };
        }

        if (desiredResource.ServiceKey != null)
        {
            spec.ServiceKey = new V1Beta1AgentConnection.SecretRef
            {
                SecretName = desiredResource.ServiceKey.Name,
                SecretKey = desiredResource.ServiceKey.Key
            };
        }

        if (desiredResource.UserName != null)
        {
            spec.UserName = new V1Beta1AgentConnection.SecretRef
            {
                SecretName = desiredResource.UserName.Name,
                SecretKey = desiredResource.UserName.Key
            };
        }

        return spec;
    }

    public async ValueTask<SecretResource> CreateConnectionSecretResource(AgentConnectionResource template, string @namespace)
    {
        var secretKeyValues = new List<SecretKeyValue>();

        if (template.Token != null)
        {
            var tokenHash = await _secretHelper.GetCachedSecretDataHashByRef(template.Token.Name, @namespace, template.Token.Key);
            if (tokenHash != null)
            {
                secretKeyValues.Add(new SecretKeyValue(ClusterDefaults.DefaultTokenSecretKey, tokenHash));
            }
        }

        if (template.UserName != null)
        {
            var usernameHash = await _secretHelper.GetCachedSecretDataHashByRef(template.UserName.Name, @namespace, template.UserName.Key);
            if (usernameHash != null)
            {
                secretKeyValues.Add(new SecretKeyValue(ClusterDefaults.DefaultUsernameSecretKey, usernameHash));
            }
        }

        if (template.ApiKey != null)
        {
            var apiKeyHash = await _secretHelper.GetCachedSecretDataHashByRef(template.ApiKey.Name, @namespace, template.ApiKey.Key);
            if (apiKeyHash != null)
            {
                secretKeyValues.Add(new SecretKeyValue(ClusterDefaults.DefaultApiKeySecretKey, apiKeyHash));
            }
        }

        if (template.ServiceKey != null)
        {
            var serviceKeyHash = await _secretHelper.GetCachedSecretDataHashByRef(template.ServiceKey.Name, @namespace, template.ServiceKey.Key);
            if (serviceKeyHash != null)
            {
                secretKeyValues.Add(new SecretKeyValue(ClusterDefaults.DefaultServiceKeySecretKey, serviceKeyHash));
            }
        }

        return new SecretResource(secretKeyValues.NormalizeSecrets());
    }

    public async ValueTask<IDictionary<string, byte[]>> CreateConnectionSecretData(AgentConnectionResource template, string @namespace)
    {
        var data = new Dictionary<string, byte[]>();

        if (template.Token != null)
        {
            var tokenData = await _secretHelper.GetLiveSecretDataByRef(template.Token.Name, @namespace, template.Token.Key);
            if (tokenData != null)
            {
                data.Add(ClusterDefaults.DefaultTokenSecretKey, tokenData);
            }
        }

        if (template.UserName != null)
        {
            var usernameData = await _secretHelper.GetLiveSecretDataByRef(template.UserName.Name, @namespace, template.UserName.Key);
            if (usernameData != null)
            {
                data.Add(ClusterDefaults.DefaultUsernameSecretKey, usernameData);
            }
        }

        if (template.ApiKey != null)
        {
            var apiKeyData = await _secretHelper.GetLiveSecretDataByRef(template.ApiKey.Name, @namespace, template.ApiKey.Key);
            if (apiKeyData != null)
            {
                data.Add(ClusterDefaults.DefaultApiKeySecretKey, apiKeyData);
            }
        }

        if (template.ServiceKey != null)
        {
            var serviceKeyData = await _secretHelper.GetLiveSecretDataByRef(template.ServiceKey.Name, @namespace, template.ServiceKey.Key);
            if (serviceKeyData != null)
            {
                data.Add(ClusterDefaults.DefaultServiceKeySecretKey, serviceKeyData);
            }
        }

        return data;
    }

}
