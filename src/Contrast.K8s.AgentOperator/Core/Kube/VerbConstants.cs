using KubeOps.Operator.Rbac;

namespace Contrast.K8s.AgentOperator.Core.Kube
{
    public static class VerbConstants
    {
        public const RbacVerb ReadAndPatch = RbacVerb.Get | RbacVerb.List | RbacVerb.Patch | RbacVerb.Watch;

        // TODO This math is wrong?
        public const RbacVerb AllButDelete = RbacVerb.All & ~RbacVerb.Delete;

        public const RbacVerb ReadOnly = RbacVerb.Get | RbacVerb.List | RbacVerb.Watch;

        public const RbacVerb FullControl = RbacVerb.All;
    }
}
