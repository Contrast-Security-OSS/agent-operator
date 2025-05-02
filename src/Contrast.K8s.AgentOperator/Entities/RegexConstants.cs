// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using JetBrains.Annotations;

namespace Contrast.K8s.AgentOperator.Entities;

public static class RegexConstants
{
    [RegexPattern]
    public const string AgentTypeRegex = @"^(dotnet-core|dotnet|java|node|nodejs|node-esm|nodejs-esm|nodejs-legacy|php|personal-home-page|python|dummy)$";

    [RegexPattern]
    public const string InjectorVersionRegex = @"^(latest|(\d+(\.\d+){0,3}(-.+)?))$";

    [RegexPattern]
    public const string PullPolicyRegex = @"^(Always|IfNotPresent|Never)$";
}
