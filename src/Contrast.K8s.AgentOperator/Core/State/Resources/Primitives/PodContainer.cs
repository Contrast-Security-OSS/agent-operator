namespace Contrast.K8s.AgentOperator.Core.State.Resources.Primitives
{
    public record PodContainer(
        string Name,
        string Image
    );
}
