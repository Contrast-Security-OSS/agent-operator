// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using MediatR;

namespace Contrast.K8s.AgentOperator.Core.Events;

public record TickChanged : INotification
{
    public static TickChanged Instance { get; } = new();
}
