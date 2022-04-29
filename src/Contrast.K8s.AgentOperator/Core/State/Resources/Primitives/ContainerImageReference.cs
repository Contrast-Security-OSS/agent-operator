namespace Contrast.K8s.AgentOperator.Core.State.Resources.Primitives
{
    public record ContainerImageReference(string Repository, string Name, string Tag)
    {
        public string GetFullyQualifiedContainerImageName() => $"{Repository}/{Name}:{Tag}".ToLowerInvariant();
    }
}
