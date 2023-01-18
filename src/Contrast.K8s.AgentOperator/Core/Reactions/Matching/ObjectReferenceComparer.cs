// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Contrast.K8s.AgentOperator.Core.Reactions.Matching
{
    public class ObjectReferenceComparer<T> : IEqualityComparer<T> where T : class
    {
        public static ObjectReferenceComparer<T> Default { get; } = new();

        public bool Equals(T? x, T? y)
        {
            return ReferenceEquals(x, y);
        }

        public int GetHashCode(T obj)
        {
            return RuntimeHelpers.GetHashCode(obj);
        }
    }
}
