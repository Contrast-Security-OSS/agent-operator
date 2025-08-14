// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using KubeOps.Abstractions.Entities.Attributes;

namespace Contrast.K8s.AgentOperator.Entities.Common;

public class ClusterNamespaceLabelSelectorSpec
{
    [Required]
    [Description("The name of the label to match. Required.")]
    public string Name { get; set; } = null!;

    [Required]
    [Description("The value of the label to match. Glob patterns are supported. Required.")]
    public string Value { get; set; } = null!;
}
