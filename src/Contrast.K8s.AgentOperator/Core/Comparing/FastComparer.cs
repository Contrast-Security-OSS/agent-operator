// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Diagnostics;
using NLog;

namespace Contrast.K8s.AgentOperator.Core.Comparing
{
    public class FastComparer
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly ObjectComparerPlanner _planner;

        public FastComparer(ObjectComparerPlanner planner)
        {
            _planner = planner;
        }

        public bool AreEqual<T>(T left, T right)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                return EqualsImpl(left, right);
            }
            finally
            {
                if (stopwatch.ElapsedMilliseconds > 40 && left != null && right != null)
                {
                    // No idea why the compiler really wants these checked for null?
                    Logger.Debug($"Comparing {left} and {right} took {stopwatch.ElapsedMilliseconds}ms.");
                }
            }
        }

        private bool EqualsImpl(object? a, object? b)
        {
            // Both null.
            if (a == null && b == null)
            {
                return true;
            }

            // One, but not the other, null.
            if (a == null || b == null)
            {
                return false;
            }

            // Same object.
            if (ReferenceEquals(a, b))
            {
                return true;
            }

            if (a is IDictionary aDict && b is IDictionary bDict)
            {
                return DictionaryEqualsImpl(aDict, bDict);
            }

            if (a is ICollection aList && b is ICollection bList)
            {
                return ListEqualsImpl(aList, bList);
            }

            var aType = a.GetType();
            var bType = b.GetType();

            // Primitive types.
            if (FastComparerHelper.IsPrimitive(aType)
                && FastComparerHelper.IsPrimitive(bType))
            {
                return Equals(a, b);
            }

            // At this point, we assume the types must be exactly the same.
            if (aType != bType)
            {
                return false;
            }

            return ObjectEqualsImpl(a, b, aType);
        }

        private bool ListEqualsImpl(ICollection aList, ICollection bList)
        {
            // Empty list.
            if (aList.Count == 0 && bList.Count == 0)
            {
                return true;
            }

            // List, different size.
            if (aList.Count != bList.Count)
            {
                return false;
            }

            // List, same size.
            var aEnumerator = aList.GetEnumerator();
            var bEnumerator = bList.GetEnumerator();

            while (aEnumerator.MoveNext()
                   && bEnumerator.MoveNext())
            {
                if (!EqualsImpl(aEnumerator.Current, bEnumerator.Current))
                {
                    return false;
                }
            }

            return true;
        }

        private bool DictionaryEqualsImpl(IDictionary aDict, IDictionary bDict)
        {
            // Empty.
            if (aDict.Count == 0 && bDict.Count == 0)
            {
                return true;
            }

            // Different size.
            if (aDict.Count != bDict.Count)
            {
                return false;
            }

            // Same size.
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var key in aDict.Keys)
            {
                if (!bDict.Contains(key))
                {
                    return false;
                }

                var aDictValue = aDict[key];
                var bDictValue = bDict[key];
                if (!EqualsImpl(aDictValue, bDictValue))
                {
                    return false;
                }
            }

            return true;
        }

        private bool ObjectEqualsImpl(object aObject, object bObject, Type type)
        {
            var plan = _planner.GetComparerPlan(type);
            foreach (var (left, right) in plan.Walk(aObject, bObject))
            {
                if (!EqualsImpl(left, right))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
