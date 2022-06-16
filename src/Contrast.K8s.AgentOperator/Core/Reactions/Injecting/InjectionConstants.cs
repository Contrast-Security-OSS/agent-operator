// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Contrast.K8s.AgentOperator.Core.Reactions.Injecting
{
    public static class InjectionConstants
    {
        public const string OperatorAttributePrefix = "agents.contrastsecurity.com/";
        public const string IsInjectedAttributeName = "agents.contrastsecurity.com/is-injected";
        public const string InjectedOnAttributeName = "agents.contrastsecurity.com/injected-on";
        public const string InjectorHashAttributeName = "agents.contrastsecurity.com/injector-hash";
        public const string InjectorNameAttributeName = "agents.contrastsecurity.com/injector-name";
        public const string InjectorNamespaceAttributeName = "agents.contrastsecurity.com/injector-namespace";
        public const string WorkloadNameAttributeName = "agents.contrastsecurity.com/workload-name";
        public const string WorkloadNamespaceAttributeName = "agents.contrastsecurity.com/workload-namespace";
    }
}
