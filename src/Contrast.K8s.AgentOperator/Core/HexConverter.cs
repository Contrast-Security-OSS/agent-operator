// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text;
using HexMate;

namespace Contrast.K8s.AgentOperator.Core;

public static class HexConverter
{
    public static string ToLowerHex(byte[] bytes)
    {
        return Convert.ToHexString(bytes, HexFormattingOptions.Lowercase);
    }

    public static string ToLowerHex(byte[] bytes, int length)
    {
        // This returns null bytes for some reason...
        //return Convert.ToHexString(bytes, 0, length, HexFormattingOptions.Lowercase);

        var hashString = new StringBuilder();
        for (var index = 0; index < bytes.Length && hashString.Length <= length; index++)
        {
            var x = bytes[index];
            hashString.Append($"{x:x2}");
        }

        return hashString.ToString();
    }
}
