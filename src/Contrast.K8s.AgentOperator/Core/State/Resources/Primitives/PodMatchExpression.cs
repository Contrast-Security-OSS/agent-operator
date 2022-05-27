using System.Collections.Generic;

namespace Contrast.K8s.AgentOperator.Core.State.Resources.Primitives
{
    public record PodMatchExpression(string Key, LabelMatchOperation Operator, IReadOnlyCollection<string> Values);
}
