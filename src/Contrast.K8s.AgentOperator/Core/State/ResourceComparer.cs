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
