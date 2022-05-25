namespace Contrast.K8s.AgentOperator.Core.State.Resources.Primitives
{
    public record ContainerImageReference(string Registry, string Name, string Tag)
    {
        public string GetFullyQualifiedContainerImageName() => $"{Registry}/{Name}:{Tag}".ToLowerInvariant();
    }
}
