// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Security.Cryptography;
using System.Text;

namespace Contrast.K8s.AgentOperator.Core.Reactions.Secrets;

public class VolumeSecrets
{
    public static string GetConnectionVolumeSecretName(string agentConnection)
    {
        return "agent-connection-volume-secret-" + GetShortHash(agentConnection);
    }

    private static string GetShortHash(string text)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(text);
        var hash = sha256.ComputeHash(bytes);
        return HexConverter.ToLowerHex(hash, 8);
    }
}
