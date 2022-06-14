using Contrast.K8s.AgentOperator.Core.State.Resources;

namespace Contrast.K8s.AgentOperator.Core.Reactions.Injecting.Patching
{
    public record PatchingContext(string WorkloadName,
                                  string WorkloadNamespace,
                                  AgentInjectorResource Injector,
                                  AgentConnectionResource Connection,
                                  AgentConfigurationResource? Configuration,
                                  string ContrastMountPath);
}
