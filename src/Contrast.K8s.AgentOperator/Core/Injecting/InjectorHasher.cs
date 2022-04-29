using System;
using System.Security.Cryptography;
using System.Text;
using Contrast.K8s.AgentOperator.Core.Kube;
using Contrast.K8s.AgentOperator.Core.State.Resources;

namespace Contrast.K8s.AgentOperator.Core.Injecting
{
    public interface IInjectorHasher
    {
        string GetHash(AgentInjectorResource resource);
    }

    public class InjectorHasher : IInjectorHasher
    {
        private readonly KubernetesJsonSerializer _jsonSerializer;

        public InjectorHasher(KubernetesJsonSerializer jsonSerializer)
        {
            _jsonSerializer = jsonSerializer;
        }

        public string GetHash(AgentInjectorResource resource)
        {
            var json = _jsonSerializer.SerializeObject(resource);
            return Sha256(json);
        }

        private static string Sha256(string text)
        {
            using var sha256 = SHA256.Create();
            return BitConverter.ToString(sha256.ComputeHash(Encoding.UTF8.GetBytes(text)));
        }
    }
}
