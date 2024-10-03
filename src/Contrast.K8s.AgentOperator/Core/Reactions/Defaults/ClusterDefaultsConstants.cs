// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Contrast.K8s.AgentOperator.Core.Reactions.Defaults;

public static class ClusterDefaultsConstants
{
    public const string ResourceManagedByAttributeName = "agents.contrastsecurity.com/managed-by";
    public const string DefaultTokenSecretKey = "token";
    public const string DefaultUsernameSecretKey = "username";
    public const string DefaultApiKeySecretKey = "api-key";
    public const string DefaultServiceKeySecretKey = "service-key";
}
