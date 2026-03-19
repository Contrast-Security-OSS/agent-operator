// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Contrast.K8s.AgentOperator.Options;

[AttributeUsage(AttributeTargets.Property)]
public sealed class RequiresMinimumKubernetesVersionAttribute : Attribute
{
    public int Major { get; }
    public int Minor { get; }

    public RequiresMinimumKubernetesVersionAttribute(int major, int minor)
    {
        Major = major;
        Minor = minor;
    }
}
