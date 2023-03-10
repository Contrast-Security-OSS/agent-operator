# Telemetry

The Contrast Agent Operator uses telemetry to collect usage data. Telemetry is collected when the operator is first installed in a cluster and then periodically (every few hours) afterwards.

Your privacy is important to us. The telemetry feature doesn't collect application data. The data is anonymized before being sent securely to Contrast. Then the aggregated data is stored encrypted and under restricted access control. Any collected data will be deleted after one year.

To opt-out of the telemetry feature, set the `CONTRAST_AGENT_TELEMETRY_OPTOUT` environment variable to `1` or `true`.

Telemetry data is securely sent to _telemetry.dotnet.contrastsecurity.com_. You can also opt out of telemetry by blocking communication at the network level.

The telemetry feature collects the following data:

## Operator v0.3.0

- The version of the operator.
- The uptime of the operator.
- Cluster version information and platform as published by the Kubernetes API.
- A cryptographically (SHA256) anonymous hash of the cluster ID, a randomly generated GUID created at first launch by the operator and stored in the operator's namespace as a Secret.
- The count of watched resources in the cluster (all operator entities, DaemonSets, DeploymentConfigs, Deployments, Namespaces, Pods, Secrets, and StatefulSets).
- Exceptions thrown internally by the operator, including log message, exception type, exception message, and stack trace frames.

## Operator v0.6.0

- Performance counters "System.Runtime" exposed by the .NET runtime.
- Count of pods injected with a Contrast agent.
