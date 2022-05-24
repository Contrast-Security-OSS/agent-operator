using System.Security.Cryptography;
using System.Text;

#nullable enable

namespace Contrast.K8s.AgentOperator.Core.Telemetry.Helpers
{
    public class Sha256Hasher
    {
        // Super lazy, copied from the CoreFX repo.

        public string Hash(string text)
        {
            using var sha256 = SHA256.Create();
            return HashInFormat(sha256, text);
        }

        private static string HashInFormat(HashAlgorithm algorithm, string text)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            var hash = algorithm.ComputeHash(bytes);
            var hashString = new StringBuilder();
            foreach (var x in hash)
            {
                hashString.AppendFormat("{0:x2}", x);
            }

            return hashString.ToString();
        }
    }
}
