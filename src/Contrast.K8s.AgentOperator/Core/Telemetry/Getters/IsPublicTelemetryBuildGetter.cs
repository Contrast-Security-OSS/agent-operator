// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

namespace Contrast.K8s.AgentOperator.Core.Telemetry.Getters;

public class IsPublicTelemetryBuildGetter
{
#if CONTRAST_IS_PUBLIC_TELEMETRY_BUILD
    private const bool PublicBuildFlag = true;
#else
    private const bool PublicBuildFlag = false;
#endif

    // ReSharper disable once MemberCanBeMadeStatic.Global
    public bool IsPublicBuild()
    {
        return PublicBuildFlag;
    }
}
