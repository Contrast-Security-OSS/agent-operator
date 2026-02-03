// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Contrast.K8s.AgentOperator.Core.State.Resources;
using Contrast.K8s.AgentOperator.Entities;
using System.Text;

namespace Contrast.K8s.AgentOperator.Core.Reactions.Defaults.Common;

public class ConfigurationSyncing
{
    public V1Beta1AgentConfiguration.AgentConfigurationSpec CreateConfigurationSpec(AgentConfigurationResource desiredResource)
    {
        var builder = new StringBuilder();
        foreach (var yamlKey in desiredResource.YamlKeys)
        {
            // Hard code the new line for Linux.
            builder.Append(yamlKey.Key).Append(": '").Append(yamlKey.Value).Append("'\n");
        }

        var yaml = builder.ToString();

        var initContainer = desiredResource.InitContainerOverrides is { } overrides
            ? new V1Beta1AgentConfiguration.InitContainerOverridesSpec
            {
                SecurityContext = overrides.SecurityContext
            }
            : null;

        return new V1Beta1AgentConfiguration.AgentConfigurationSpec
        {
            Yaml = yaml,
            SuppressDefaultApplicationName = desiredResource.SuppressDefaultApplicationName,
            SuppressDefaultServerName = desiredResource.SuppressDefaultServerName,
            EnableYamlVariableReplacement = desiredResource.EnableYamlVariableReplacement,
            InitContainer = initContainer
        };
    }
}
