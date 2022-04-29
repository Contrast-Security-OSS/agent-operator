using MediatR;

namespace Contrast.K8s.AgentOperator.Core.Events
{
    public record ElectedLeader : INotification;
}
