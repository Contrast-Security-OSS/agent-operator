// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using k8s;
using k8s.Models;
using KubeOps.Operator.Caching;

namespace Contrast.K8s.AgentOperator.Core.Extensions
{
    public class NoOpResourceCacheDecorator<TEntity> : IResourceCache<TEntity> where TEntity : IKubernetesObject<V1ObjectMeta>
    {
        private readonly IResourceCache<TEntity> _inner;

        public NoOpResourceCacheDecorator(IResourceCache<TEntity> inner)
        {
            _inner = inner;
        }

        public TEntity Get(string id)
        {
            throw new System.NotImplementedException();
        }

        public TEntity Upsert(TEntity resource, out CacheComparisonResult result)
        {
            // Always considered the object new/modified.
            result = CacheComparisonResult.Other;
            return resource;
        }

        public void Fill(IEnumerable<TEntity> resources)
        {
        }

        public void Remove(TEntity resource)
        {
        }

        public void Clear()
        {
        }
    }
}
