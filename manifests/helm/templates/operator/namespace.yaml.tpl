{{ if ne .Values.operator.enabled false }}
{{ if .Values.namespace }}
kind: Namespace
apiVersion: v1
metadata:
  name: '{{ .Values.namespace }}'
  labels:
    app.kubernetes.io/part-of: contrast-agent-operator
{{ end }}
{{ end }}
