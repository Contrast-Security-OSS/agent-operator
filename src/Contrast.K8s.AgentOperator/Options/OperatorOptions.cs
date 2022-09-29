// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Contrast.K8s.AgentOperator.Options
{
    public record OperatorOptions(string Namespace,
                                  string FieldManagerName = "agents.contrastsecurity.com",
                                  int SettlingDurationSeconds = 10,
                                  int EventQueueSize = 10 * 1024);
}
