// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Contrast.K8s.AgentOperator.Core.State.Resources.Interfaces;

namespace Contrast.K8s.AgentOperator.Core.Comparing
{
    public class FastResourceComparer : IResourceComparer
    {
        private readonly FastComparer _comparer;

        public FastResourceComparer(FastComparer comparer)
        {
            _comparer = comparer;
        }

        public bool AreEqual<T>(T left, T right) where T : INamespacedResource?
        {
            return _comparer.AreEqual(left, right);
        }
    }
}
