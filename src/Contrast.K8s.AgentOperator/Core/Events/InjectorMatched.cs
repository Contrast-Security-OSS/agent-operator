using Contrast.K8s.AgentOperator.Core.State;
using Contrast.K8s.AgentOperator.Core.State.Resources;
using Contrast.K8s.AgentOperator.Core.State.Resources.Interfaces;
using MediatR;

namespace Contrast.K8s.AgentOperator.Core.Events
{
    public record InjectorMatched(ResourceIdentityPair<IResourceWithPodTemplate> Target,
                                  ResourceIdentityPair<AgentInjectorResource>? Injector) : INotification;
}
