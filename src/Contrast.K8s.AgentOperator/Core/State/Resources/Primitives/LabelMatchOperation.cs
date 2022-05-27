namespace Contrast.K8s.AgentOperator.Core.State.Resources.Primitives
{
    public enum LabelMatchOperation
    {
        Unknown = 0,
        In,
        NotIn,
        Exists,
        DoesNotExist
    }
}
