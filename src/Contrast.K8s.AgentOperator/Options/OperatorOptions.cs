// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Threading.Channels;

namespace Contrast.K8s.AgentOperator.Options
{
    public record OperatorOptions(string Namespace,
                                  int SettlingDurationSeconds,
                                  int EventQueueSize,
                                  BoundedChannelFullMode EventQueueFullMode,
                                  int EventQueueMergeWindowSeconds,
                                  bool RunInitContainersAsNonRoot,
                                  bool SuppressSeccompProfile,
                                  string FieldManagerName = "agents.contrastsecurity.com");
}
