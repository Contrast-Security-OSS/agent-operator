{{- define "imagePullSecret" }}
{{- with .Values.imageCredentials }}
{{- printf "{\"auths\":{\"%s\":{\"username\":\"%s\",\"password\":\"%s\",\"email\":\"%s\",\"auth\":\"%s\"}}}" .registry .username .password .email (printf "%s:%s" .username .password | b64enc) | b64enc }}
{{- end }}
{{- end }}
{{ if .Values.imageCredentials.enabled }}
apiVersion: v1
kind: Secret
metadata:
  name: {{ .Values.imageCredentials.pullSecretName }}
  namespace: >-
    {{ .Values.namespace }}
type: kubernetes.io/dockerconfigjson
data:
  .dockerconfigjson: {{ template "imagePullSecret" . }}
{{ end }}
