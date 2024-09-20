{{/*
Determine namespaces applicable for deploying the agent injectors
*/}}
{{- define "contrast-agent-operator.filterInjectorNamespaces" -}}
{{- $namespaceNames := list }}
{{- if .Values.agentInjectors.lookupNamespaces.deployToAllAccessibleNamespaces }}
  {{- $namespaces := lookup "v1" "Namespace" "" "" }}
  {{- if $namespaces.items }}
    {{- range $ns := $namespaces.items}}
      {{- $include := true }}
      {{- range $index, $exclude := default (list "gatekeeper*" "kube*") $.Values.agentInjectors.lookupNamespaces.excludePatterns }}
        {{- if regexMatch $exclude $ns.metadata.name }}
        {{- $include = false}}
        {{- end }}
      {{- end }}
      {{- if $include }}
        {{- $namespaceNames = append $namespaceNames $ns.metadata.name }}
      {{- end }}
    {{- end }}
  {{- else }}
    {{- $namespaceNames = list "dry-run-namespace-not-representative-of-reality" }}
  {{- end }}
{{- else }}
  {{- $namespaceNames = default (list .Release.Namespace) .Values.agentInjectors.namespaces -}}
{{- end }}
{{ toJson $namespaceNames }}
{{- end }}