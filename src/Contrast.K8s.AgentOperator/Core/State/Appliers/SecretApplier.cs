// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.State.Resources;
using Contrast.K8s.AgentOperator.Core.State.Resources.Primitives;
using JetBrains.Annotations;
using k8s.Models;
using MediatR;

namespace Contrast.K8s.AgentOperator.Core.State.Appliers
{
    [UsedImplicitly]
    public class SecretApplier : BaseApplier<V1Secret, SecretResource>
    {
        public SecretApplier(IStateContainer stateContainer, IMediator mediator) : base(stateContainer, mediator)
        {
        }

        public override ValueTask<SecretResource> CreateFrom(V1Secret entity, CancellationToken cancellationToken = default)
        {
            var data = entity.Data ?? new Dictionary<string, byte[]>();
            var keyPairs = data.Select(x => new SecretKeyValue(x.Key, Sha256(x.Value))).NormalizeSecrets();

            var resource = new SecretResource(keyPairs);
            return ValueTask.FromResult(resource);
        }

        private static string Sha256(byte[] data)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(data);
            return HexConverter.ToLowerHex(bytes);
        }
    }
}
