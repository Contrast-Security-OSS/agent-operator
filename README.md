# agent-operator

A K8s operator to inject agents into existing K8s workloads. Images are built in [agent-operator-images](https://github.com/Contrast-Security-Inc/agent-operator-images).

Managed by the .NET team.

For questions, suggestions, bugs, see [#agent-operator](https://contrastsecurityinc.slack.com/archives/C03FNADV430).

Supported Versions:

| Kubernetes Version | OpenShift Version | Supported | End-of-Support |
|--------------------|-------------------|-----------|----------------|
| v1.23              | v4.10             | Yes       | 2023-02-28     |
| v1.22              | v4.9              | Yes       | 2022-10-28     |
| v1.21              | v4.8              | Yes       | 2022-06-28     |

## Artifacts

Builds released into the the `internal` environment are published to our internal container image registry hosted on Azure (which expire after 30 days). Manifest are uploaded as GitHub Action artifacts (which expire after 7 days).

```
contrastdotnet.azurecr.io/agent-operator/agent-operator:0.0.1
contrastdotnet.azurecr.io/agent-operator/agent-operator:0.0
contrastdotnet.azurecr.io/agent-operator/agent-operator:0
contrastdotnet.azurecr.io/agent-operator/agent-operator:latest
```

Builds released into the the `public` environment are are published to DockerHub. Manifest are uploaded to the [GitHub releases page](https://github.com/Contrast-Security-Inc/agent-operator/releases).

```
contrast/agent-operator:1.0.0
contrast/agent-operator:1.0
contrast/agent-operator:1
contrast/agent-operator:latest
```

## Design

Data flow is unidirectional when possible.

![Data Flow](./docs/assets/data-flow.png)

## Development

See [./docs/development.md](./docs/development.md)
