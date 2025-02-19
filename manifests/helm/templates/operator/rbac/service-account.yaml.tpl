{{ if ne .Values.operator.enabled false }}
apiVersion: v1
kind: ServiceAccount
metadata:
  name: contrast-agent-operator-service-account
  namespace: '{{ default .Release.Namespace .Values.namespace }}'
  labels:
    app.kubernetes.io/name: operator
    app.kubernetes.io/part-of: contrast-agent-operator
{{ end }}
