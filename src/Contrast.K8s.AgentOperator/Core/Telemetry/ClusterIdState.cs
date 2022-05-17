namespace Contrast.K8s.AgentOperator.Core.Telemetry
{
    public interface IClusterIdState
    {
        ClusterId? GetClusterId();
        bool SetClusterId(ClusterId clusterId);
    }

    public class ClusterIdState : IClusterIdState
    {
        private ClusterId? _cache;

        public ClusterId? GetClusterId()
        {
            return _cache;
        }

        public bool SetClusterId(ClusterId clusterId)
        {
            var lastId = _cache;
            _cache = clusterId;

            var updated = lastId != _cache;
            return updated;
        }
    }
}
