// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.State;
using Contrast.K8s.AgentOperator.Core.State.Resources;
using Contrast.K8s.AgentOperator.Core.State.Resources.Interfaces;

namespace Contrast.K8s.AgentOperator.Core.Reactions.Defaults
{
    public class ClusterDefaults
    {
        private readonly IStateContainer _state;

        public ClusterDefaults(IStateContainer state)
        {
            _state = state;
        }

        public string GetDefaultAgentConfigurationName(string targetNamespace)
        {
            return "default-agent-configuration-" + GetShortHash(targetNamespace);
        }

        public string GetDefaultAgentConnectionName(string targetNamespace)
        {
            return "default-agent-connection-" + GetShortHash(targetNamespace);
        }

        public string GetDefaultAgentConnectionSecretName(string targetNamespace)
        {
            return "default-agent-connection-secret-" + GetShortHash(targetNamespace);
        }

        public Dictionary<string, string> GetAnnotationsForManagedResources(string templateResourceName, string templateResourceNamespace)
        {
            return new Dictionary<string, string>
            {
                { ClusterDefaultsConstants.ResourceManagedByAttributeName, $"{templateResourceNamespace}/{templateResourceName}" },
            };
        }

        public async Task<IReadOnlyCollection<string>> GetAllNamespaces(CancellationToken cancellationToken = default)
        {
            var namespaces = new HashSet<string>();
            var keys = await _state.GetKeysByType<INamespacedResource>(cancellationToken);
            foreach (var id in keys)
            {
                var @namespace = id.Namespace.ToLowerInvariant();
                namespaces.Add(@namespace);
            }

            return namespaces;
        }

        public async Task<IReadOnlyCollection<string>> GetValidNamespacesForDefaults(CancellationToken cancellationToken = default)
        {
            var namespaces = new HashSet<string>();
            var keys = await _state.GetKeysByType<AgentInjectorResource>(cancellationToken);
            foreach (var id in keys)
            {
                var @namespace = id.Namespace.ToLowerInvariant();
                namespaces.Add(@namespace);
            }

            return namespaces;
        }

        private static string GetShortHash(string text)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(text);
            var hash = sha256.ComputeHash(bytes);
            var hashString = new StringBuilder();
            for (var index = 0; index < hash.Length && hashString.Length <= 8; index++)
            {
                var x = hash[index];
                hashString.AppendFormat("{0:x2}", x);
            }

            return hashString.ToString();
        }
    }
}
