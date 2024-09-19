// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using k8s.Models;
using System.Collections.Generic;

namespace Contrast.K8s.AgentOperator.Core.Reactions.Injecting.Patching;

public record VariableReplacement(string Value, IList<V1EnvVar> AdditionalEnvVars);
