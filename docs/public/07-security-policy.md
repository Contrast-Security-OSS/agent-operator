# Security Policy

The Contrast Agent Operator supports clusters with various security policies. This functionality was first added in `v0.15.0`.

## Supported Implementations

### Pod Security Admission

The Agent Operator supports executing in Kubernetes clusters that make use of [Pod Security Standards](https://kubernetes.io/docs/concepts/security/pod-security-standards/) using [Pod Security Admission](https://kubernetes.io/docs/concepts/security/pod-security-admission/) (first stable in v1.25). The operator and any injections are tested against the `Restricted` policy under `enforce` mode (`latest` version).

If upgrading from operator versions `< v0.15.0`, existing clusters must opt-in to this feature by applying `CONTRAST_RUN_INIT_CONTAINER_AS_NON_ROOT=true` to the operator workload. For new installations, no change is necessary. See [Additional Notes](#additional-notes).

> [Pod Security Policies](https://kubernetes.io/docs/concepts/security/pod-security-policy/) (deprecated in Kubernetes v1.21) are not supported. Clusters should migrate to [Pod Security Admission](https://kubernetes.io/docs/concepts/security/pod-security-admission/) to enforce the Pod Security Standards.

### OpenShift Security Context Constraints

The Agent Operator supports executing in OpenShift clusters that make use of [security context constraints (SCCs)](https://docs.openshift.com/container-platform/4.12/authentication/managing-security-context-constraints.html) using the built-in admission/mutating controllers. The operator and any injections are tested against the `restricted` policy.

If running in an OpenShift cluster where the default `restricted` policy disallows setting a seccomp policy, ensure `CONTRAST_SUPPRESS_SECCOMP_PROFILE=true` is applied to the operator workload.

### Third-party Admission Plugins

Third-party, security policy, admission plugins may work with the Contrast Agent Operator, but any interaction is not tested or officially supported at this time.

## Additional Notes

When `CONTRAST_RUN_INIT_CONTAINER_AS_NON_ROOT=true` is applied or when executing in an OpenShift environment, the following minimum agent versions must be used:

| Type             | Minimum Version |
|------------------|-----------------|
| `dotnet-core`    | `2.4.4`         |
| `java`           | `4.11.0`        |
| `nodejs`         | `5.1.0`         |
| `nodejs-legacy`  | `4.30.0`        |
| `php`            | `1.8.0`         |
| `python`         | `7.2.0`         |
