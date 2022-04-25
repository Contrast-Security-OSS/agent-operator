using JetBrains.Annotations;

namespace Contrast.K8s.AgentOperator.Entities
{
    public static class RegexConstants
    {
        [RegexPattern]
        public const string AgentTypeRegex = @"^(dotnet-core)$";

        [RegexPattern]
        public const string InjectorVersion = @"^(latest)$";
    }
}
