using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Contrast.K8s.AgentOperator.Core.State.Resources;
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

            var resource = new SecretResource(
                data.Keys.ToList(),
                Sha256(data.Values.Select(x => x ?? Array.Empty<byte>()))
            );
            return ValueTask.FromResult(resource);
        }

        private static string Sha256(IEnumerable<byte[]> data)
        {
            var builder = new StringBuilder();
            foreach (var d in data)
            {
                builder.Append(Sha256(d));
            }

            return Sha256(Encoding.UTF8.GetBytes(builder.ToString()));
        }

        private static string Sha256(byte[] data)
        {
            using var sha256 = SHA256.Create();

            var hash = new StringBuilder();
            var bytes = sha256.ComputeHash(data);

            foreach (var b in bytes)
            {
                hash.Append(b.ToString("x2"));
            }

            return hash.ToString();
        }
    }
}
