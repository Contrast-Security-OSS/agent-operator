namespace Contrast.K8s.AgentOperator.Core.State.Resources.Primitives
{
    public record PodInjectionConvergenceCondition(string Status, string Reason, string Message);
}
