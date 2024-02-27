// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Contrast.K8s.AgentOperator.Core.State.Resources;

namespace Contrast.K8s.AgentOperator.Core.Reactions.Injecting.Patching;

public record PatchingContext(string WorkloadName,
                              string WorkloadNamespace,
                              AgentInjectorResource Injector,
                              AgentConnectionResource Connection,
                              AgentConfigurationResource? Configuration,
                              string AgentMountPath,
                              string WritableMountPath);
