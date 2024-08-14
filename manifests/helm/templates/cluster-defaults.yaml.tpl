{{ if .Values.clusterDefaults.enabled }}
apiVersion: agents.contrastsecurity.com/v1beta1
kind: ClusterAgentConfiguration
metadata:
  name: default-agent-configuration
  namespace: >-
    {{ if not .Values.createNamespace }}{{.Release.Namespace}}{{else}}{{.Values.namespace}}{{end}}
spec:
  template:
    spec:
      yaml: |-
{{ .Values.clusterDefaults.yaml | indent 8 }}
---
apiVersion: agents.contrastsecurity.com/v1beta1
kind: ClusterAgentConnection
metadata:
  name: default-agent-connection
  namespace: >-
    {{ if not .Values.createNamespace }}{{.Release.Namespace}}{{else}}{{.Values.namespace}}{{end}}
spec:
  template:
    spec:
      url: >-
        {{ required "The key clusterDefaults.clusterDefaults must be set if clusterDefaults.enabled is true" .Values.clusterDefaults.url }}
      apiKey:
        secretName: {{ .Values.clusterDefaults.existingSecret | default "default-agent-connection-secret" }}
        secretKey: apiKey
      serviceKey:
        secretName: {{ .Values.clusterDefaults.existingSecret | default "default-agent-connection-secret" }}
        secretKey: serviceKey
      userName:
        secretName: {{ .Values.clusterDefaults.existingSecret | default "default-agent-connection-secret" }}
        secretKey: userName
---
{{if not .Values.clusterDefaults.existingSecret }}
apiVersion: v1
kind: Secret
metadata:
  name: default-agent-connection-secret
  namespace: >-
    {{ if not .Values.createNamespace }}{{.Release.Namespace}}{{else}}{{.Values.namespace}}{{end}}
type: Opaque
stringData:
  apiKey: >-
    {{ required "The key clusterDefaults.apiKeyValue must be set if clusterDefaults.enabled is true and clusterDefaults.existingSecret is not set" .Values.clusterDefaults.apiKeyValue }}
  serviceKey: >-
    {{ required "The key clusterDefaults.serviceKeyValue must be set if clusterDefaults.enabled is true and clusterDefaults.existingSecret is not set" .Values.clusterDefaults.serviceKeyValue }}
  userName: >-
    {{ required "The key clusterDefaults.userNameValue must be set if clusterDefaults.enabled is true and clusterDefaults.existingSecret is not set" .Values.clusterDefaults.userNameValue }}
{{ end }}
{{ end }}
