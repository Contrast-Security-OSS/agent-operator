// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Contrast.K8s.AgentOperator.Core.State.Resources.Interfaces;
using KellermanSoftware.CompareNetObjects;
using NLog;

namespace Contrast.K8s.AgentOperator.Core.State
{
    public interface IResourceComparer
    {
        bool AreEqual<T>(T left, T right) where T : INamespacedResource?;
    }

    public class ResourceComparer : IResourceComparer
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly CompareLogic _compareLogic = new(new ComparisonConfig
        {
            MaxDifferences = 1
        });

        public bool AreEqual<T>(T left, T right) where T : INamespacedResource?
        {
            var result = _compareLogic.Compare(left, right);
            if (result.ElapsedMilliseconds > 40)
            {
                Logger.Debug($"Comparing {left} and {right} took {result.ElapsedMilliseconds}ms.");
            }

            return result.AreEqual;
        }
    }
}
