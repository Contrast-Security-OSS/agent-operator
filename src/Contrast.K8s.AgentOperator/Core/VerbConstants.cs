using KubeOps.Operator.Rbac;

namespace Contrast.K8s.AgentOperator.Core
{
    public static class VerbConstants
    {
        public const RbacVerb ReadAndPatch = RbacVerb.Get | RbacVerb.List | RbacVerb.Patch | RbacVerb.Watch;

        public const RbacVerb ReadOnly = RbacVerb.Get | RbacVerb.List | RbacVerb.Watch;
    }
}
