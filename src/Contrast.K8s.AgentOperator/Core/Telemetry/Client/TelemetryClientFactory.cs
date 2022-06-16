// Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using RestEase;

namespace Contrast.K8s.AgentOperator.Core.Telemetry.Client
{
    public interface ITelemetryClientFactory
    {
        ITelemetryClient Create();
    }

    public class TelemetryClientFactory : ITelemetryClientFactory
    {
        private const string TelemetryUri = "https://telemetry.dotnet.contrastsecurity.com";
        private readonly TelemetryState _state;

        public TelemetryClientFactory(TelemetryState state)
        {
            _state = state;
        }

        public ITelemetryClient Create()
        {
            var client = RestClient.For<ITelemetryClient>(TelemetryUri);
            client.UserAgent = $"AgentOperator/{_state.OperatorVersion}";
            return client;
        }
    }
}
