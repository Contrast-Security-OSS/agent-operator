using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Contrast.K8s.AgentOperator.Core.State.Resources.Primitives;

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
    }
}
