namespace Contrast.K8s.AgentOperator.Core
{
    public static class OperatorVersion
    {
        public static string Version => typeof(OperatorVersion).Assembly.GetName().Version?.ToString() ?? "0.0.2";
    }
}
