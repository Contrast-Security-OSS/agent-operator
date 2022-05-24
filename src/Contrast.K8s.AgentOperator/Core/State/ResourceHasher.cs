﻿using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Contrast.K8s.AgentOperator.Core.Kube;
using Contrast.K8s.AgentOperator.Core.State.Resources;

namespace Contrast.K8s.AgentOperator.Core.State
{
    public interface IResourceHasher
    {
        string GetHash(AgentInjectorResource injector,
                       AgentConnectionResource connection,
                       AgentConfigurationResource? configuration,
                       IEnumerable<SecretResource> secretResources);
    }

    public class ResourceHasher : IResourceHasher
    {
        private readonly KubernetesJsonSerializer _jsonSerializer;

        public ResourceHasher(KubernetesJsonSerializer jsonSerializer)
        {
            _jsonSerializer = jsonSerializer;
        }

        public string GetHash(AgentInjectorResource injector,
                              AgentConnectionResource connection,
                              AgentConfigurationResource? configuration,
                              IEnumerable<SecretResource> secretResources)
        {
            return GetHashImpl(injector, connection, configuration, secretResources);
        }

        private string GetHashImpl(params object?[] objects)
        {
            var builder = new StringBuilder();
            foreach (var o in objects)
            {
                builder.Append(GetHashImpl(o));
            }

            return Sha256(builder.ToString());
        }

        private string GetHashImpl(object? o)
        {
            var json = _jsonSerializer.SerializeObject(o ?? "<null>");
            return Sha256(json);
        }

        private static string Sha256(string text)
        {
            using var sha256 = SHA256.Create();

            var hash = new StringBuilder();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(text));

            foreach (var b in bytes)
            {
                hash.Append(b.ToString("x2"));
            }

            return hash.ToString();
        }
    }
}