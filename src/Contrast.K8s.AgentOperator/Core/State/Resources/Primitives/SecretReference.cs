namespace Contrast.K8s.AgentOperator.Core.State.Resources.Primitives
{
    public record SecretReference(
        string Namespace,
        string Name,
        string Key
    );
}
