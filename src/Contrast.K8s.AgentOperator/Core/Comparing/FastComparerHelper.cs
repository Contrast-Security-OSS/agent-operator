// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Contrast.K8s.AgentOperator.Core.Comparing
{
    public static class FastComparerHelper
    {
        private static readonly HashSet<Type> PrimitiveTypes = new()
        {
            typeof(decimal),
            typeof(string),
            typeof(DateTime),
            typeof(DateTimeOffset),
            typeof(Guid),
        };

        public static bool IsPrimitive(Type type)
        {
            return type.IsPrimitive
                   || type.IsEnum
                   || PrimitiveTypes.Contains(type)
                   || IsNullablePrimitive(type);
        }

        private static bool IsNullablePrimitive(Type type)
        {
            return type.IsGenericType
                   && type.GetGenericTypeDefinition() == typeof(Nullable<>)
                   && IsPrimitive(type.GetGenericArguments()[0]);
        }
    }
}
