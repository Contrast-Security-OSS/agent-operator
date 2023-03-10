# Introduction

> Important
>
> This feature is in beta. Beta status means the feature might change or act unexpectedly. By using this feature, you agree to the [Contrast Beta Terms and Conditions](https://docs.contrastsecurity.com/en/beta-terms-and-conditions.html "Contrast Beta Terms and Conditions").

The Contrast Agent Operator is a standard [Kubernetes operator](https://kubernetes.io/docs/concepts/extend-kubernetes/operator/) that executes within Kubernetes and OpenShift clusters to automate injecting Contrast agents into existing workloads, configuring injected agents, and facilitating agent upgrades.

See the [setup](./setup/01-installation.md) section to get started, or take a look at a [full example](./04-full-example.md).

The operator is configured using declarative Kubernetes native resource types. Resources types are documented in the [configuration reference](./03-configuration-reference.md) section.
