// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using KubeOps.Abstractions.Rbac;

namespace Contrast.K8s.AgentOperator.Core.Kube;

public static class VerbConstants
{
    public const RbacVerb ReadAndPatch = RbacVerb.Get | RbacVerb.List | RbacVerb.Patch | RbacVerb.Watch;

    public const RbacVerb AllButDelete =
        RbacVerb.Get | RbacVerb.List | RbacVerb.Watch | RbacVerb.Create | RbacVerb.Update | RbacVerb.Patch;

    public const RbacVerb ReadOnly = RbacVerb.Get | RbacVerb.List | RbacVerb.Watch;

    public const RbacVerb FullControl = RbacVerb.AllExplicit;
}
