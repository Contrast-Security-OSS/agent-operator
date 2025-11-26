// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Security.Cryptography;
using System.Text;

namespace Contrast.K8s.AgentOperator.Core;

public static class HashHelper
{
    public static string GetShortHash(string text)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(text);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexStringLower(hash, 0, 8);
    }

    public static string Sha256(string text)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(text));
        return Convert.ToHexStringLower(bytes);
    }

    public static string Sha256(byte[] data)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(data);
        return Convert.ToHexStringLower(bytes);
    }
}
