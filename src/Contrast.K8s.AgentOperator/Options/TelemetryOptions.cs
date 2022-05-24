namespace Contrast.K8s.AgentOperator.Options
{
    public record TelemetryOptions(string ClusterIdSecretName,
                                   string ClusterIdSecretNamespace);
}
