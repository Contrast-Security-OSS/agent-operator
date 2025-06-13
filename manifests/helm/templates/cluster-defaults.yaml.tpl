{{ if .Values.clusterDefaults.enabled }}
apiVersion: agents.contrastsecurity.com/v1beta1
kind: ClusterAgentConfiguration
metadata:
  name: default-agent-configuration
  namespace: '{{ default .Release.Namespace .Values.namespace }}'
spec:
  template:
    spec:
      enableYamlVariableReplacement: {{ .Values.clusterDefaults.enableYamlVariableReplacement | default false }}
      suppressDefaultApplicationName: {{ .Values.clusterDefaults.suppressDefaultApplicationName | default false }}
      suppressDefaultServerName: {{ .Values.clusterDefaults.suppressDefaultServerName | default false }}
      yaml: |-
{{ .Values.clusterDefaults.yaml | indent 8 }}
---
apiVersion: agents.contrastsecurity.com/v1beta1
kind: ClusterAgentConnection
metadata:
  name: default-agent-connection
  namespace: '{{ default .Release.Namespace .Values.namespace }}'
spec:
  template:
    spec:
      mountAsVolume: {{ .Values.clusterDefaults.mountConnectionAsVolume | default false }}
{{ if or .Values.clusterDefaults.existingTokenSecret .Values.clusterDefaults.tokenValue }}
      token:
        secretName: {{ .Values.clusterDefaults.existingTokenSecret | default "default-agent-connection-token-secret" }}
        secretKey: token
{{ else }}
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
{{ end }}
---
{{ if and (not .Values.clusterDefaults.existingTokenSecret) (not .Values.clusterDefaults.existingSecret) }}
{{ if .Values.clusterDefaults.tokenValue  }}
apiVersion: v1
kind: Secret
metadata:
  name: default-agent-connection-token-secret
  namespace: >-
    {{ .Values.namespace }}
type: Opaque
stringData:
  token: >-
    {{ required "The key clusterDefaults.tokenValue must be set if clusterDefaults.enabled is true and clusterDefaults.existingTokenSecret is not set" .Values.clusterDefaults.tokenValue }}
{{ else }}
apiVersion: v1
kind: Secret
metadata:
  name: default-agent-connection-secret
  namespace: '{{ default .Release.Namespace .Values.namespace }}'
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
{{ end }}
