// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Contrast.K8s.AgentOperator.Core.State.Resources.Primitives;

public record SecretKeyValue(string Key, string DataHash)
{
    public static SecretKeyValue Create(string key, byte[] value)
    {
        return new SecretKeyValue(key, HashHelper.Sha256(value));
    }

}
