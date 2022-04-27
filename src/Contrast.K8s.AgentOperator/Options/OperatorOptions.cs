namespace Contrast.K8s.AgentOperator.Options
{
    public record OperatorOptions(string Namespace, string FieldManagerName = "agents.contrastsecurity.com");
}
