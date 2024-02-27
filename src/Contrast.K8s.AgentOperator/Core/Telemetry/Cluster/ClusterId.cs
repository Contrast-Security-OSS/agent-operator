// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Contrast.K8s.AgentOperator.Core.Telemetry.Cluster;

public record ClusterId(Guid Guid, DateTimeOffset CreatedOn)
{
    public static ClusterId NewId()
    {
        return new ClusterId(Guid.NewGuid(), DateTimeOffset.Now);
    }
}
