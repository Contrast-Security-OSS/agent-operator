// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using k8s.Models;
using KubeOps.Abstractions.Entities;
using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace Contrast.K8s.AgentOperator.Core.Kube;

public static class EntityMetadataCache
{
    private static readonly ConcurrentDictionary<Type, EntityMetadata> MetadataCache = new();

    public static EntityMetadata GetMetadata<TEntity>()
    {
        var type = typeof(TEntity);
        return MetadataCache.GetOrAdd(type, CreateMetadata);
    }

    private static EntityMetadata CreateMetadata(Type resourceType)
    {
        var attribute = resourceType.GetCustomAttribute<KubernetesEntityAttribute>();
        if (attribute == null)
        {
            throw new ArgumentException($"The Type {resourceType} does not have the kubernetes entity attribute.");
        }

        var kind = string.IsNullOrWhiteSpace(attribute.Kind) ? resourceType.Name : attribute.Kind;
        var version = string.IsNullOrWhiteSpace(attribute.ApiVersion) ? "v1" : attribute.ApiVersion;

        return new EntityMetadata(
            kind,
            version,
            attribute.Group,
            attribute.PluralName);
    }
}
