{{ if ne .Values.operator.enabled false }}
{{ if .Values.namespace }}
kind: Namespace
apiVersion: v1
metadata:
  name: '{{ default .Release.Namespace .Values.namespace }}'
  labels:
    app.kubernetes.io/part-of: contrast-agent-operator
{{ end }}
{{ end }}
