// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Contrast.K8s.AgentOperator.Core.State.Resources.Primitives;
using k8s.Models;

namespace Contrast.K8s.AgentOperator.Core
{
    public static class CollectionExtensions
    {
        public static bool TryGetSingle<T>(this IEnumerable<T> collection, Func<T, bool> predicate, [NotNullWhen(true)] out T? value)
        {
            var selected = collection.SingleOrDefault(predicate);
            value = selected;
            return selected != null;
        }

        public static bool TryGetAnnotation(this IEnumerable<MetadataAnnotations> collection, string name, [NotNullWhen(true)] out string? value)
        {
            var selected = collection.SingleOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
            value = selected?.Value;
            return value != null;
        }

        public static string? GetAnnotation(this IEnumerable<MetadataAnnotations> collection, string name)
        {
            return collection.SingleOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase))?.Value;
        }

        public static bool ContainsAnnotation(this IEnumerable<MetadataAnnotations> collection, string name)
        {
            return collection.Any(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        public static IReadOnlyCollection<SecretKeyValue> NormalizeSecrets(this IEnumerable<SecretKeyValue> secretKeyValues)
        {
            return secretKeyValues.OrderBy(x => x.Key).ToList();
        }

        public static void SetOrRemove(this IDictionary<string, string> dictionary, string key, string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                if (dictionary.ContainsKey(key))
                {
                    dictionary.Remove(key);
                }
            }
            else
            {
                dictionary[key] = value;
            }
        }

        public static void RemovePrefixed(this IDictionary<string, string> dictionary, string prefix)
        {
            foreach (var key in dictionary.Keys.Where(x => x.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList())
            {
                dictionary.Remove(key);
            }
        }

        public static void AddOrUpdate<T>(this ICollection<T> collection, Func<T, bool> predicate, T value)
        {
            var existing = collection.FirstOrDefault(predicate);
            if (existing != null)
            {
                collection.Remove(existing);
            }

            collection.Add(value);
        }

        public static void AddOrUpdate(this ICollection<V1Container> collection, string name, V1Container value)
        {
            collection.AddOrUpdate(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase), value);
        }

        public static void AddOrUpdate(this ICollection<V1Volume> collection, string name, V1Volume value)
        {
            collection.AddOrUpdate(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase), value);
        }

        public static void AddOrUpdate(this ICollection<V1VolumeMount> collection, string name, V1VolumeMount value)
        {
            collection.AddOrUpdate(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase), value);
        }

        public static void AddOrUpdate(this ICollection<V1EnvVar> collection, string name, V1EnvVar value)
        {
            collection.AddOrUpdate(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase), value);
        }

        public static void AddOrUpdate(this ICollection<V1EnvVar> collection, V1EnvVar value)
        {
            collection.AddOrUpdate(x => string.Equals(x.Name, value.Name, StringComparison.OrdinalIgnoreCase), value);
        }

        public static void Remove<T>(this ICollection<T> collection, Func<T, bool> predicate)
        {
            foreach (var resource in collection.Where(predicate))
            {
                collection.Remove(resource);
            }
        }
    }
}
