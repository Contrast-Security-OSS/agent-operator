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
  {{- if or $injector.selector $injector.images }}
  {{ $selector := $injector.selector | default dict -}}
  selector:
  {{- if $selector.labels }}
    labels:
      {{- $selector.labels | toYaml | nindent 6 }}
  {{- end }}
  {{- if $selector.images }}
    images:
      {{- $selector.images | toYaml | nindent 6 }}
  {{- end }}
  {{- end }}
{{- end }}
{{- end }}
{{ end }}