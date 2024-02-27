// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Contrast.K8s.AgentOperator.Core.State.Resources.Primitives;

public record ResourceWithPodSpecSelector(
    IReadOnlyCollection<string> ImagesPatterns,
    IReadOnlyCollection<LabelPattern> LabelPatterns,
    IReadOnlyCollection<string> Namespaces);
