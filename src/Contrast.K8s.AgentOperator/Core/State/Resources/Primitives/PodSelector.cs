using System.Collections.Generic;

namespace Contrast.K8s.AgentOperator.Core.State.Resources.Primitives
{
    public record PodSelector(IReadOnlyCollection<PodMatchExpression> Expressions);
}
