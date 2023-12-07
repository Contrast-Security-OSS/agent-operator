{{ if .Values.agentInjectors.enabled }}
{{- range $injector := .Values.agentInjectors.injectors }}
{{- range $namespace := .namespaces }}
---
apiVersion: agents.contrastsecurity.com/v1beta1
kind: AgentInjector
metadata:
  name: {{ $injector.name }}
  namespace: {{ $namespace }}
spec:
  {{- if eq $injector.enabled false}}
  enabled: false
  {{- end }}
  {{- if $injector.connectionName }}
  connection:
    name: {{ $injector.connectionName }}
  {{- end}}
  {{- if $injector.configurationName }}
  configuration:
    name: {{ $injector.configurationName }}
  {{- end }}
  {{- if $injector.imageVersion }}
  version: {{ quote $injector.imageVersion }}
  {{- end }}
  type: {{ $injector.language }}
  {{- if $injector.image }}
  image:
    {{- $injector.image | toYaml | nindent 4 }}
  {{- end}}
  {{ $selector := $injector.selector | default dict -}}
  {{- $labels := $selector.labels -}}
  {{- $images := $selector.images -}}
  {{- $_ := required "One of injector.selector.labels or injector.selector.images required" (coalesce $labels $images) -}}
  selector:
  {{- if $labels }}
    labels:
      {{- $labels | toYaml | nindent 6 }}
  {{- end }}
  {{- if $images }}
    images:
      {{- $images | toYaml | nindent 6 }}
  {{- end }}
{{- end }}
{{- end }}
{{ end }}