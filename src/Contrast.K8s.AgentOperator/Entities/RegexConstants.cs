using JetBrains.Annotations;

namespace Contrast.K8s.AgentOperator.Entities
{
    public static class RegexConstants
    {
        [RegexPattern]
        public const string AgentTypeRegex = @"^(dotnet-core|java)$";

        [RegexPattern]
        public const string InjectorVersionRegex = @"^(latest|(\d+(\.\d+){0,3}))$";
    }
}
