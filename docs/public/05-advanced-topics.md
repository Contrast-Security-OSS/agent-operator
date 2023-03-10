# Advanced topics

## .NET Core chaining support

The .NET Core Agent, paired with the Contrast Agent Operator, supports profiler chaining with Dynatrace using the [Dynatrace Operator](https://github.com/Dynatrace/dynatrace-operator). Support is enabled automatically when the Dynatrace Operator is used to inject the Dynatrace agent into workloads.

| Vendor    | Version         | Support Validated On |
|-----------|-----------------|----------------------|
| Dynatrace | Operator v0.6.0 | 2022/06/09           |

Future Dynatrace versions may break chaining. Chaining can introduce incompatibilities and can be disabled using the `agent.dotnet.enable_chaining: false` option.
