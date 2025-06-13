// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Security.Cryptography;

namespace Contrast.K8s.AgentOperator.Core.State.Resources.Primitives;

public record SecretKeyValue(string Key, string DataHash)
{
    public static SecretKeyValue Create(string key, byte[] value)
    {
        return new SecretKeyValue(key, Sha256(value));
    }
    private static string Sha256(byte[] data)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(data);
        return HexConverter.ToLowerHex(bytes);
    }
}
