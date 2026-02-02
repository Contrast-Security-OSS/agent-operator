// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Contrast.K8s.AgentOperator.Core.Reactions.Secrets;

public class VolumeSecrets
{
    public const string ConfigVolumeSecretKey = "contrast_security.yaml";

    public static string GetConnectionVolumeSecretName(string agentConnection)
    {
        return "agent-connection-volume-secret-" + HashHelper.GetShortHash(agentConnection);
    }
}
