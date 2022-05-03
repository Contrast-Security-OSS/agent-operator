namespace Contrast.K8s.AgentOperator.Core.Injecting
{
    public static class InjectionConstants
    {
        public const string OperatorAttributePrefix = "agents.contrastsecurity.com/";
        public const string IsInjectedAttributeName = "agents.contrastsecurity.com/is-injected";
        public const string InjectedOnAttributeName = "agents.contrastsecurity.com/injected-on";
        public const string HashAttributeName = "agents.contrastsecurity.com/injector-hash";
        public const string NameAttributeName = "agents.contrastsecurity.com/injector-name";
        public const string NamespaceAttributeName = "agents.contrastsecurity.com/injector-namespace";
    }
}
