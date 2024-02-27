// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sigil;

namespace Contrast.K8s.AgentOperator.Core.Comparing;

public delegate object Accessor(object thisObject);

public class ObjectComparerPlanner
{
    private readonly ConcurrentDictionary<Type, ObjectComparerPlan> _plans = new();

    public ObjectComparerPlan GetComparerPlan(Type type)
    {
        try
        {
            return _plans.GetOrAdd(type, ObjectComparerPlanImpl);
        }
        catch (Exception e)
        {
            throw new Exception($"Failed to build plan for type '{type.FullName}'.", e);
        }
    }

    private static ObjectComparerPlan ObjectComparerPlanImpl(Type type)
    {
        var set = new HashSet<Accessor>();

        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var info in properties)
        {
            if (info.CanRead && info.GetMethod is { } getMethod)
            {
                var needsBox = getMethod.ReturnType.IsValueType;
                var emiter = needsBox
                    ? Emit<Accessor>.NewDynamicMethod()
                                    .LoadArgument(0)
                                    .CastClass(type)
                                    .Call(getMethod)
                                    .Box(getMethod.ReturnType)
                                    .Return()
                    : Emit<Accessor>.NewDynamicMethod()
                                    .LoadArgument(0)
                                    .CastClass(type)
                                    .Call(getMethod)
                                    .CastClass<object>()
                                    .Return();

                var accessor = emiter.CreateDelegate();
                set.Add(accessor);
            }
        }

        // Be space optimized.
        var accessors = set.ToArray();

        return new ObjectComparerPlan(accessors);
    }
}

public class ObjectComparerPlan
{
    private readonly IReadOnlyList<Accessor> _accessors;

    public ObjectComparerPlan(IReadOnlyList<Accessor> accessors)
    {
        _accessors = accessors;
    }

    public IEnumerable<(object Left, object Right)> Walk(object left, object right)
    {
        // Reduce allocations on this hot path.
        // ReSharper disable once ForCanBeConvertedToForeach
        for (var i = 0; i < _accessors.Count; i++)
        {
            var accessor = _accessors[i];
            var leftValue = accessor.Invoke(left);
            var rightValue = accessor.Invoke(right);

            yield return (leftValue, rightValue);
        }
    }
}
