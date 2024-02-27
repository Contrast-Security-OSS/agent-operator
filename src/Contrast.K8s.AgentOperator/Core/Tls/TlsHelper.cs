// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Contrast.K8s.AgentOperator.Core.Tls;

public static class TlsHelper
{
    public static byte[] GenerateSansHash(IEnumerable<string> sans)
    {
        // This string needs to be stable.
        var normalizedList = sans.Select(x=>x.ToLowerInvariant())
                                 .DistinctBy(x => x, StringComparer.Ordinal)
                                 .OrderBy(x => x);

        var sansStr = string.Join(";", normalizedList);
        return Sha256(sansStr);
    }

    private static byte[] Sha256(string text)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(text));
        return bytes;
    }
}
