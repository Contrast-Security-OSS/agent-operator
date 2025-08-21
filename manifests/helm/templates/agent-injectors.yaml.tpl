{{ if .Values.agentInjectors.enabled }}
{{ if .Values.agentInjectors.useClusterAgentInjectors }} # ClusterAgentInjectors
{{- range $injector := .Values.agentInjectors.injectors }}
---
apiVersion: agents.contrastsecurity.com/v1beta1
kind: ClusterAgentInjector
metadata:
  name: {{ $injector.name }}
  namespace: '{{ default $.Release.Namespace $.Values.namespace }}'
spec:
  {{- if $.Values.agentInjectors.namespaces }}
  namespaces: {{- $.Values.agentInjectors.namespaces | toYaml | nindent 4 }}
  {{- end }}
  {{- if $.Values.agentInjectors.namespaceLabelSelector }}
  namespaceLabelSelector: {{- $.Values.agentInjectors.namespaceLabelSelector | toYaml | nindent 4 }}
  {{- end }}
  template:
    spec:
      {{- if eq $injector.enabled false}}
      enabled: false
      {{- end }}
      {{- if $injector.imageVersion }}
      version: {{ quote $injector.imageVersion }}
      {{- end }}
      type: {{ $injector.language }}
      {{- if $injector.image }}
      image:
        {{- $injector.image | toYaml | nindent 8 }}
      {{- end}}
      {{- if or $injector.selector $injector.images }}
      {{ $selector := $injector.selector | default dict -}}
      selector:
      {{- if $selector.labels }}
        labels:
          {{- $selector.labels | toYaml | nindent 10 }}
      {{- end }}
      {{- if $selector.images }}
        images:
          {{- $selector.images | toYaml | nindent 10 }}
      {{- end }}
      {{- end }}
{{- end }}
{{ else }} # AgentInjectors
{{- range $namespace := .Values.agentInjectors.namespaces }}
{{- range $injector := .Values.agentInjectors.injectors }}
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
{{ end }}
