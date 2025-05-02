# Supported technologies

## Kubernetes/OpenShift Support

| Kubernetes Version | OpenShift Version | Operator Version | End-of-Support |
|--------------------|-------------------|------------------|----------------|
| v1.31              |                   | v0.14.0+         | 2025-10-28     |
| v1.30              |                   | v0.14.0+         | 2025-06-28     |
| v1.29              | v4.16             | v0.14.0+         | 2025-02-28     |
| v1.28              | v4.15             | v0.14.0+         | 2024-10-28     |
| v1.27              | v4.14             | v0.14.0+         | 2024-06-28     |
| v1.26              | v4.13             | v0.14.0+         | 2024-02-28     |

- The Contrast Agent Operator follows the upstream [Kubernetes community support policy](https://kubernetes.io/releases/patch-releases/#support-period). End-of-life dates are documented on the [Kubernetes releases](https://kubernetes.io/releases/#release-history) page.
- OpenShift support is dependent on the included version of Kubernetes. For example, OpenShift v4.10 uses Kubernetes v1.23 and will be supported by Contrast until 2023-02-28. See Red Hat's [support article](https://access.redhat.com/solutions/4870701) for the mapping between Kubernetes and OpenShift versions.
- The Contrast Agent Operator only supports executing on Linux amd64/arm64 hosts and will refuse to be scheduled onto incompatible nodes. Additionally, the operator only supports injecting workloads running on Linux amd64/arm64 hosts, even if the Contrast Agent supports additional platforms. Contact [Contrast Support](https://support.contrastsecurity.com/hc/en-us) if Kubernetes on Windows support is desired.

## Agent types

| Agent                 | Agent Type     | Support Status | Compatibility Notes                                                                                            |
|-----------------------|----------------|----------------|----------------------------------------------------------------------------------------------------------------|
| .NET Core             | dotnet-core    | Supported      | [Supported .NET Core technologies](https://docs.contrastsecurity.com/en/-net-core-supported-technologies.html) |
| Java                  | java           | Supported      | [Supported Java technologies](https://docs.contrastsecurity.com/en/java-supported-technologies.html)           |
| NodeJS                | nodejs         | Beta           | [Supported NodeJS technologies](https://docs.contrastsecurity.com/en/node-js-supported-technologies.html )     |
| NodeJS Legacy         | nodejs-legacy  | Beta           | [Supported NodeJS technologies](https://docs.contrastsecurity.com/en/node-js-supported-technologies.html )     |
| PHP                   | php            | Beta           | [Supported PHP technologies](https://docs.contrastsecurity.com/en/php-supported-technologies.html)             |
| Python                | python         | Beta           | [Supported Python technologies](https://docs.contrastsecurity.com/en/python-supported-technologies.html)       |

- Injection of NodeJS and PHP applications is in beta. Beta status means the feature might change or act unexpectedly. By using this feature, you agree to the [Contrast Beta Terms and Conditions](https://docs.contrastsecurity.com/en/beta-terms-and-conditions.html "Contrast Beta Terms and Conditions").
- Injection of the NodeJS Agent may result in a substantial increase in the startup time of the instrumented application. If startup time is unacceptable, injecting the agent during compilation may be desireable. If the application was injected by the NodeJS agent during compilation then injection during runtime by the operator should be disabled. See the [rewriter CLI](https://docs.contrastsecurity.com/en/node-js-agent-rewriter-cli.html) for more information.
