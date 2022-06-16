// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Contrast.K8s.AgentOperator.Core
{
    public static class OperatorVersion
    {
        public static string Version => typeof(OperatorVersion).Assembly.GetName().Version?.ToString() ?? "0.0.2";
    }
}
