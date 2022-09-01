{{ if .Values.clusterDefaults.enabled }}
apiVersion: agents.contrastsecurity.com/v1beta1
kind: ClusterAgentConfiguration
metadata:
  name: default-agent-configuration
  namespace: >-
    {{ .Values.namespace }}
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
    {{ .Values.namespace }}
spec:
  template:
    spec:
      url: >-
        {{ required "The key clusterDefaults.clusterDefaults must be set if clusterDefaults.enabled is true" .Values.clusterDefaults.url }}
      apiKey:
        secretName: default-agent-connection-secret
        secretKey: apiKey
      serviceKey:
        secretName: default-agent-connection-secret
        secretKey: serviceKey
      userName:
        secretName: default-agent-connection-secret
        secretKey: userName
---
apiVersion: v1
kind: Secret
metadata:
  name: default-agent-connection-secret
  namespace: >-
    {{ .Values.namespace }}
type: Opaque
stringData:
  apiKey: >-
    {{ required "The key clusterDefaults.apiKeyValue must be set if clusterDefaults.enabled is true" .Values.clusterDefaults.apiKeyValue }}
  serviceKey: >-
    {{ required "The key clusterDefaults.serviceKeyValue must be set if clusterDefaults.enabled is true" .Values.clusterDefaults.serviceKeyValue }}
  userName: >-
    {{ required "The key clusterDefaults.userNameValue must be set if clusterDefaults.enabled is true" .Values.clusterDefaults.userNameValue }}
{{ end }}
